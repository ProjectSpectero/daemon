using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using ServiceStack;
using ServiceStack.OrmLite;
using Spectero.daemon.Libraries.Config;
using Spectero.daemon.Libraries.Core.Constants;
using Spectero.daemon.Libraries.Core.OutgoingIPResolver;
using Spectero.daemon.Libraries.Core.Statistics;
using Spectero.daemon.Libraries.Services;
using Spectero.daemon.Libraries.Services.HTTPProxy;
using Spectero.daemon.Libraries.Services.OpenVPN;
using Spectero.daemon.Libraries.Services.OpenVPN.Elements;
using Spectero.daemon.Models;
using Spectero.daemon.Models.Opaque.Requests;
using Messages = Spectero.daemon.Libraries.Core.Constants.Messages;
using Utility = Spectero.daemon.Libraries.Core.Utility;

namespace Spectero.daemon.Controllers
{
    [Microsoft.AspNetCore.Mvc.Route("v1/[controller]")]
    [ApiExplorerSettings(IgnoreApi = false, GroupName = nameof(ServiceController))]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public class ServiceController : BaseController
    {
        private readonly IServiceManager _serviceManager;
        private readonly IServiceConfigManager _serviceConfigManager;
        private readonly IOutgoingIPResolver _outgoingIpResolver;

        public ServiceController(IOptionsSnapshot<AppConfig> appConfig, ILogger<ServiceController> logger,
            IDbConnection db, IServiceManager serviceManager,
            IServiceConfigManager serviceConfigManager, IStatistician statistician,
            IOutgoingIPResolver outgoingIpResolver)
            : base(appConfig, logger, db)
        {
            _serviceManager = serviceManager;
            _serviceConfigManager = serviceConfigManager;
            _outgoingIpResolver = outgoingIpResolver;
        }

        [HttpGet("", Name = "IndexServices")]
        public IActionResult Index()
        {
            var services = _serviceManager.GetServices();
            var ret = new Dictionary<string, string>();
            foreach (var service in services)
            {
                ret.Add(service.Value.GetType().Name, service.Value.GetState().ToString());
            }
            _response.Result = ret;
            return Ok(_response);
        }

        [HttpGet("{name}/{task}", Name = "ManageServices")]
        public IActionResult Manage(string name, string task)
        {
            if (Defaults.ValidServices.Any(s => name == s) &&
                Defaults.ValidActions.Any(s => task == s))
            {
                if (task.Equals("config"))
                {
                    var type = Utility.GetServiceType(name);
                    var config = _serviceConfigManager.Generate(type);
                    _response.Result = config;
                    return Ok(_response);
                }

                var message = _serviceManager.Process(name, task, out var error);

                if (error != null)
                {
                    _response.Errors.Add(message, error);
                    return StatusCode(500, _response);
                }                 

                _response.Message = message;
                return Ok(_response);
            }

            _response.Errors.Add(Errors.INVALID_SERVICE_OR_ACTION_ATTEMPT, "");
            return BadRequest(_response);
        }

        [HttpPut("OpenVPN/config", Name = "HandleOpenVPNConfigUpdate")]
        public async Task<IActionResult> HandleOpenVPNConfigUpdate([FromBody] OpenVPNConfigUpdateRequest config)
        {
            if (!ModelState.IsValid || !config.Validate(out var errors))
            {
                _response.Errors.Add(Errors.VALIDATION_FAILED, errors);
                return BadRequest(_response);
            }
            
            // OK bob, the basic schema is valid. Let's do some semantics checks now.
            // There is also no need to coppy result.ErrorMessages out into our buffer this time. If we're here, that means it all passed already.
	        
            // Let's check the listeners.
            var networksAlreadySeen = new List<IPNetwork>();
            
            var listenersAlreadySeen = new Dictionary<int,
                List<OpenVPNListener>>();
	        
            var errorEncountered = false;
	        
            foreach (var listener in config.Listeners)
            {
                var parsedNetwork = IPNetwork.Parse(listener.Network);
                var parsedAddress = IPAddress.Parse(listener.IPAddress);

                foreach (var network in networksAlreadySeen)
                {
                    Logger.LogDebug($"Checking if {network} overlaps with any already defined networks: {networksAlreadySeen}");
                    // Uh oh, we got an overlap. No bueno.
                    if (! network.Contains(parsedNetwork) && ! network.Equals(parsedNetwork)) continue;;
			        
                    _response.Errors.Add(Errors.FIELD_OVERLAP, $"listeners.network:{network},{parsedNetwork}");
                    errorEncountered = true;
                    break;
                }
                
                networksAlreadySeen.Add(parsedNetwork);
		        
                // If we got here, that means listeners do not have network overlaps.
                if (!errorEncountered)
                {
                    // Now, let's check for port overlaps / same listener being defined multiple times
                    listenersAlreadySeen.TryGetValue(listener.Port.Value, out var listOfexistingListenersOnPort);

                    // OK, there are other listeners on this port.
                    if (listOfexistingListenersOnPort != null)
                    {
                        foreach (var abstractedListener in listOfexistingListenersOnPort)
                        {
                            if (!abstractedListener.Protocol.Equals(listener.Protocol))
                            {
                                Logger.LogDebug("Protocols do not match, continuing search for conflicts.");
                                continue;
                            }
                            
                            if (abstractedListener.IPAddress.Equals(IPAddress.Any.ToString()))
                            {
                                _response.Errors.Add(Errors.PORT_CONFLICT_FOUND, "0.0.0.0");
                                errorEncountered = true;
                                break;
                            }

                            // Duplicate listener found
                            if (abstractedListener.IPAddress.Equals(listener.IPAddress))
                            {
                                _response.Errors.Add(Errors.DUPLICATE_IP_AS_LISTENER_REQUEST, listener.IPAddress);
                                errorEncountered = true;
                                break;
                            }
                        }
                        
                        // If we got here, it was sufficiently unique.
                        listOfexistingListenersOnPort.Add(listener);
                    }
                    else
                    {
                        listOfexistingListenersOnPort = new List<OpenVPNListener> {listener};
                        listenersAlreadySeen.Add(listener.Port.Value, listOfexistingListenersOnPort);
                    }
                }
            }

            if (HasErrors())
            {
                return BadRequest(_response);
            }

            var baseConfig = new OpenVPNConfig (null, null)
            {
                AllowMultipleConnectionsFromSameClient = config.AllowMultipleConnectionsFromSameClient.Value,
                ClientToClient = config.ClientToClient.Value,
                MaxClients = config.MaxClients.Value,
                PushedNetworks = config.PushedNetworks as List<string>,
                RedirectGateway = config.RedirectGateway as List<RedirectGatewayOptions>,
                DhcpOptions = config.DhcpOptions as List<Tuple<DhcpOptions, string>>, 
            };

            var allListeners = config.Listeners as List<OpenVPNListener>;

            // Let's commit it all to DB as required.
            await ConfigUtils.CreateOrUpdateConfig(Db, ConfigKeys.OpenVPNBaseConfig, JsonConvert.SerializeObject(baseConfig));
            await ConfigUtils.CreateOrUpdateConfig(Db, ConfigKeys.OpenVPNListeners, JsonConvert.SerializeObject(allListeners));
            
            // Now, we need to figure out if service restart will be needed.
            // However, since this is a 3rd party daemon -- our work is way easier. We can't make ANY changes at runtime.
            // TODO: Do a config diff before doing this, but that's skipped for now.
            
            var targetServiceType = typeof(OpenVPN);

            var service = _serviceManager.GetService(targetServiceType);
            
            // Let's get the parsed config out of the DB, and update svc state if needed.
            var reconciledConfig = _serviceConfigManager.Generate(targetServiceType);
            
            if (service.GetState() == ServiceState.Running)
            {
                // Yep, restart will be needed.
                _response.Message = Messages.SERVICE_RESTART_NEEDED;
            }
            
            service.SetConfig(reconciledConfig);
           
            _response.Result = reconciledConfig;
            
            return Ok(_response);
        }

        [HttpPut("HTTPProxy/config", Name = "HandleHTTPProxyConfigUpdate")]
        public async Task<IActionResult> HandleHttpProxyConfigUpdate([FromBody] HTTPConfig config)
        {
            if (! ModelState.IsValid || config.listeners.IsNullOrEmpty())
            {
                // ModelState takes care of checking if the field map succeeded. This means mode | allowed.d | banned.d do not need manual checking
                _response.Errors.Add(Errors.MISSING_BODY, "");
                return BadRequest(_response);
            }
                
            var currentConfig = (HTTPConfig) _serviceConfigManager.Generate(Utility.GetServiceType("HTTPProxy")).First();

            var localAvailableIPs = Utility.GetLocalIPs();
            var availableIPs = localAvailableIPs as IPAddress[] ?? localAvailableIPs.ToArray();

            var consumedListenerMap = new Dictionary<int, List<IPAddress>>();

            // Check if all listeners are valid
            foreach (var listener in config.listeners)
            {
                if (IPAddress.TryParse(listener.Item1, out var holder))
                {
                    var ipChecked = AppConfig.BindToUnbound || availableIPs.Contains(holder) || holder.Equals(IPAddress.Any);
                    var portChecked = listener.Item2 > 1023 && listener.Item2 < 65535;

                    consumedListenerMap.TryGetValue(listener.Item2, out var existingListOfAddresses);

                    // DAEM-58 compliance: prevent unicast.any listeners if port is not entirely free.
                    if (existingListOfAddresses != null)
                        foreach (var ipAddress in existingListOfAddresses)
                        {
                            if (ipAddress.Equals(IPAddress.Any))
                            {
                                _response.Errors.Add(Errors.PORT_CONFLICT_FOUND, "0.0.0.0");
                                break;
                            }

                            // Duplicate listener found
                            if (ipAddress.Equals(holder))
                            {
                                _response.Errors.Add(Errors.DUPLICATE_IP_AS_LISTENER_REQUEST, listener.Item1);
                                break;
                            }
                        }

                    if (!ipChecked)
                    {
                        _response.Errors.Add(Errors.INVALID_IP_AS_LISTENER_REQUEST, listener.Item1);
                        break;
                    }


                    if (!portChecked)
                    {
                        _response.Errors.Add(Errors.INVALID_PORT_AS_LISTENER_REQUEST, listener.Item2);
                        break;
                    }

                    if (existingListOfAddresses == null)
                    {
                        existingListOfAddresses = new List<IPAddress>();

                        // If it was null, that means it wasn't in the dict either.
                        consumedListenerMap.Add(listener.Item2, existingListOfAddresses);
                    }                            

                    // List is guaranteed not null at this stage, add the IP to it.
                    existingListOfAddresses.Add(holder);
                }
                else
                {
                    _response.Errors.Add(Errors.MALFORMED_IP_AS_LISTENER_REQUEST, listener.Item1);
                    break;
                }
                   
            }

            if (HasErrors())
            {
                Logger.LogError("CCHH: Invalid listener request found.");
                return BadRequest(_response);
            }

            if (config.listeners != currentConfig.listeners ||
                config.proxyMode != currentConfig.proxyMode ||
                config.allowedDomains != currentConfig.allowedDomains ||
                config.bannedDomains != currentConfig.bannedDomains)
            {
                // A difference was found between the running config and the candidate config
                // If listener config changed AND service is running, the service needs restarting as it can't be adjusted without the sockets being re-initialized.         
                var service = _serviceManager.GetService(typeof(HTTPProxy));
                var restartNeeded = !config.listeners.SequenceEqual(currentConfig.listeners) && service.GetState() == ServiceState.Running;

                service.SetConfig(new List<IServiceConfig> { config }, restartNeeded); //Update the running config, listener config will not apply until a full system restart is made. There's a bug here.

                if (restartNeeded)
                    _response.Message = Messages.SERVICE_RESTART_NEEDED;
            }

            // If we get to this point, it means the candidate config was valid and should be committed into the DB.

            var dbConfig = await Db.SingleAsync<Configuration>(x => x.Key == ConfigKeys.HttpConfig);
            dbConfig.Value = JsonConvert.SerializeObject(config);
            await Db.UpdateAsync(dbConfig);

            _response.Result = config;
            return Ok(_response);
        }

        [HttpGet("ips", Name = "GetLocalIPs")]
        public async Task<IActionResult> GetLocalIPs()
        {
            var ips = Utility.GetLocalIPs(AppConfig.IgnoreRFC1918);
            var addresses = ips.Select(ip => ip.ToString()).ToList();

            if (addresses.Count == 0)
            {
                var ip = await _outgoingIpResolver.Resolve();
                addresses.Add(ip.ToString());
            }
               
            _response.Result = addresses;
            return Ok(_response);
        }
    }
}