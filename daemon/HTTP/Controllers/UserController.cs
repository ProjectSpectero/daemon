/*
    Spectero Daemon - Daemon Component to the Spectero Solution
    Copyright (C)  2017 Spectero, Inc.

    Spectero Daemon is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    Spectero Daemon is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.
    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <https://github.com/ProjectSpectero/daemon/blob/master/LICENSE>.
*/
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Org.BouncyCastle.Asn1.X509;
using RazorLight;
using ServiceStack;
using ServiceStack.OrmLite;
using Spectero.daemon.Libraries.Config;
using Spectero.daemon.Libraries.Core;
using Spectero.daemon.Libraries.Core.Authenticator;
using Spectero.daemon.Libraries.Core.Constants;
using Spectero.daemon.Libraries.Core.Crypto;
using Spectero.daemon.Libraries.Core.Identity;
using Spectero.daemon.Libraries.Core.OutgoingIPResolver;
using Spectero.daemon.Libraries.Errors;
using Spectero.daemon.Libraries.Services;
using Spectero.daemon.Libraries.Services.HTTPProxy;
using Spectero.daemon.Libraries.Services.OpenVPN;
using Spectero.daemon.Libraries.Services.OpenVPN.Elements;
using Spectero.daemon.Models;
using Messages = Spectero.daemon.Libraries.Core.Constants.Messages;

namespace Spectero.daemon.HTTP.Controllers
{
    [Microsoft.AspNetCore.Mvc.Route("v1/[controller]")]
    [ApiExplorerSettings(IgnoreApi = false, GroupName = nameof(UserController))]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public class UserController : BaseController
    {
        private readonly IMemoryCache _cache;
        private readonly ICryptoService _cryptoService;
        private readonly IServiceConfigManager _serviceConfigManager;
        private readonly IOutgoingIPResolver _ipResolver;
        private readonly IRazorLightEngine _razorLightEngine;
        private readonly IIdentityProvider _identityProvider;

        public UserController(IOptionsSnapshot<AppConfig> appConfig, ILogger<UserController> logger,
            IDbConnection db, IMemoryCache cache,
            ICryptoService cryptoService, IIdentityProvider identityProvider,
            IServiceConfigManager serviceConfigManager, IOutgoingIPResolver ipResolver,
            IRazorLightEngine razorLightEngine)
            : base(appConfig, logger, db)
        {
            _cache = cache;
            _cryptoService = cryptoService;
            _identityProvider = identityProvider;
            _serviceConfigManager = serviceConfigManager;
            _ipResolver = ipResolver;
            _razorLightEngine = razorLightEngine;
        }

        [HttpPost("", Name = "CreateUser")]
        public async Task<IActionResult> Create([FromBody] User user)
        {
            if (ModelState.IsValid)
            {
                // Invoked with default operation = create param
                if (!user.Validate(out var validationErrors))
                    _response.Errors.Add(Errors.VALIDATION_FAILED, validationErrors);

                if (user.AuthKey.Equals(AppConfig.CloudConnectDefaultAuthKey))
                    _response.Errors.Add(Errors.RESOURCE_RESERVED, "");
            }
            else
                _response.Errors.Add(Errors.MISSING_BODY, "");

            if (HasErrors())
                return BadRequest(_response);

            if ((user.HasRole(Models.User.Role.SuperAdmin) ||
                 user.HasRole(Models.User.Role.WebApi)) && !CurrentUser().HasRole(Models.User.Role.SuperAdmin))
            {
                // Privilege escalation attempt, shut it down.
                _response.Errors.Add(Errors.ROLE_ESCALATION_FAILED, "");
                return StatusCode(403, _response);
            }


            long userId = -1;
            user.CreatedDate = DateTime.Now;
            user.Source = Models.User.SourceTypes.Local;

            if (!user.AuthKey.ToLower().Equals(user.AuthKey))
            {
                user.AuthKey = user.AuthKey.ToLower();
                _response.Message = Messages.USER_AUTHKEY_FLATTENED;
            }

            // Check if the user would like their certificate encrypted.
            if (user.EncryptCertificate.HasValue && user.EncryptCertificate.Value)
                user.CertKey = PasswordUtils.GeneratePassword(48, 6);
            else
                user.CertKey = null;

            var userCertBytes = _cryptoService.IssueUserChain(user.AuthKey, new[] {KeyPurposeID.IdKPClientAuth}, user.CertKey);

            user.Cert = Convert.ToBase64String(userCertBytes);

            var existingUser = await Db.SingleAsync<User>(x => x.AuthKey == user.AuthKey);
            if (existingUser != null)
            {
                // Some user already exists with this authkey, let's bail
                _response.Errors.Add(Errors.CRUD_OPERATION_FAILED, "");
                _response.Errors.Add(Errors.VALIDATION_FAILED,
                    new[] {Errors.RESOURCE_EXISTS + ":authKey:" + user.AuthKey});
                return StatusCode(409, _response);
            }

            try
            {
                userId = await Db.InsertAsync(user, true);
            }
            catch (DbException e)
            {
                Logger.LogError(e.Message);
                _response.Errors.Add(Errors.OBJECT_PERSIST_FAILED, e.Message);
            }

            if (HasErrors())
                return BadRequest(_response);

            user.Id = userId;
            _response.Result = user;

            return Created(Url.RouteUrl("GetUserById", new {id = userId}), _response);
        }

        [HttpGet("self", Name = "GetCurrentUserByAuthToken")]
        public IActionResult GetCurrentUserByAuthToken()
        {
            _response.Result = CurrentUser();
            return Ok(_response);
        }


        [HttpGet("{id}", Name = "GetUserById")]
        public async Task<IActionResult> GetUserById(long id)
        {
            var user = await Db.SingleByIdAsync<User>(id);
            if (user != null)
            {
                _response.Result = user;
                return Ok(_response);
            }
            else
                return NotFound(_response);
        }

        [HttpGet("", Name = "GetUsers")]
        public async Task<IActionResult> GetUsers()
        {
            _response.Result = await Db.SelectAsync<User>();
            return Ok(_response);
        }

        // This method "ensures" that there are at least n users left of a specific role.
        // It's invoked to enforce certain constraints like "there must always be at least one SuperAdmin defined."
        private async Task<int> EnsureRoleCount(User.Role role, int count)
        {
            // Counter placeholder variable.
            var superAdmins = 0;

            // Iterate over each user and count where superadmin.
            foreach (var user in await Db.SelectAsync<User>())
                if (user.HasRole(role))
                    superAdmins++;

            // Check if there is enough superadmins.
            Logger.LogDebug($"There must be at least {count} users with the {role} role, found: {superAdmins}");
            
            if (superAdmins < count)
                throw new Exception(string.Format("There must be at least {0} users of {1} role.", count, role));
            
            return count;
        }
        
        [HttpPut("{id}", Name = "UpdateUser")]
        public async Task<IActionResult> UpdateUser(long id, [FromBody] User user)
        {
            User fetchedUser = null;

            // Check to see if the model state is valid.
            if (ModelState.IsValid)
            {
                if (!user.Validate(out var validationErrors, CRUDOperation.Update))
                    _response.Errors.Add(Errors.VALIDATION_FAILED, validationErrors);

                // Check if unprohibited auth key.
                if (user.AuthKey.Equals(AppConfig.CloudConnectDefaultAuthKey))
                    _response.Errors.Add(Errors.RESOURCE_RESERVED, "");
            }
            else
            {
                // Received no body.
                _response.Errors.Add(Errors.MISSING_BODY, "");
            }

            // Check to see for pre-existing errors.
            if (HasErrors()) return BadRequest(_response);

            // Get the provided user.
            fetchedUser = await Db.SingleByIdAsync<User>(id);

            // Check to see if we were able to get the provided user.
            if (fetchedUser == null)
            {
                // User was not found.
                _response.Errors.Add(Errors.USER_NOT_FOUND, "");
                return StatusCode(404, _response);
            }

            // Check to see if the user can alter.
            if (fetchedUser.Source.Equals(Models.User.SourceTypes.SpecteroCloud))
            {
                _response.Errors.Add(Errors.CLOUD_USER_ALTER_NOT_ALLOWED, "");
                return StatusCode(403, _response);
            }

            // Not allowed to edit an existing superadmin if you aren't one
            if (fetchedUser.HasRole(Models.User.Role.SuperAdmin) && !CurrentUser().HasRole(Models.User.Role.SuperAdmin))
            {
                _response.Errors.Add(Errors.ROLE_VALIDATION_FAILED, "");
                return StatusCode(403, _response);
            }

            // Make sure the authkey isn't undefined.
            if (!user.AuthKey.IsNullOrEmpty() && !fetchedUser.AuthKey.Equals(user.AuthKey))
            {
                if (!user.AuthKey.ToLower().Equals(user.AuthKey))
                {
                    user.AuthKey = user.AuthKey.ToLower();
                    _response.Message = Messages.USER_AUTHKEY_FLATTENED;
                }

                // Update the AuthKey.
                fetchedUser.AuthKey = user.AuthKey;
            }

            // Quick Validaation
            if (!user.Password.IsNullOrEmpty()) fetchedUser.Password = user.Password;
            if (!user.FullName.IsNullOrEmpty()) fetchedUser.FullName = user.FullName;
            if (!user.EmailAddress.IsNullOrEmpty()) fetchedUser.EmailAddress = user.EmailAddress;
            
            // The List<Role> is not equal between the two objects, i.e: changes have been proposed.
            if (!user.Roles.SequenceEqual(fetchedUser.Roles))
            {
                // No need to care about roles unless they're changing
                Logger.LogDebug("UU: Datastore roles and requested roles are different.");

                if (user.HasRole(Models.User.Role.WebApi) && (!fetchedUser.HasRole(Models.User.Role.WebApi) && !CurrentUser().HasRole(Models.User.Role.SuperAdmin)))
                    _response.Errors.Add(Errors.ROLE_VALIDATION_FAILED, "");

                if (user.HasRole(Models.User.Role.SuperAdmin) && (!fetchedUser.HasRole(Models.User.Role.SuperAdmin) && !CurrentUser().HasRole(Models.User.Role.SuperAdmin)))
                    _response.Errors.Add(Errors.ROLE_VALIDATION_FAILED, "");

                if (HasErrors())
                    return StatusCode(403, _response);

                // This means that the old (fetched) user was a SuperAdmin, but the newly proposed (user) is not one.
                // Effectively, it means that a SuperAdmin is being removed. To be able to do so, we need n -1 = 1, thus n = 2
                if (!user.HasRole(Models.User.Role.SuperAdmin) && fetchedUser.HasRole(Models.User.Role.SuperAdmin))
                    await EnsureRoleCount(Models.User.Role.SuperAdmin, 2);
                
                fetchedUser.Roles = user.Roles;
            }

            // Try to asynchronously update the database, in a synchronous manner to wait for the thread.
            try
            {
                await Db.UpdateAsync(fetchedUser);
            }
            catch (DbException e)
            {
                // Log the error to the console.
                Logger.LogError(e.Message);

                // Poor man's fluent validation, fix later. Here's to hoping DB validation actually works.
                _response.Errors.Add(Errors.VALIDATION_FAILED, e.Message);
            }

            // Make sure there are no database users.
            if (HasErrors()) return BadRequest(_response);

            // Store the response result.
            _response.Result = fetchedUser;

            // Purge the cache and remove the authkey from memory.
            ClearUserFromCacheIfExists(fetchedUser.AuthKey);

            // Return a healthy response.
            return Ok(_response);
        }
        
        [HttpDelete("{id}", Name = "DeleteUser")]
        public async Task<IActionResult> DeleteUser(long id)
        {
            // Placeholder
            User user = null;

            // Try to get the user from the database.
            user = await Db.SingleByIdAsync<User>(id);

            // Check if successful, 404 if not found.
            if (user == null) return NotFound(_response);
            
            // Prevent deletion of cloud users
            if (user.Source.Equals(Models.User.SourceTypes.SpecteroCloud))
                _response.Errors.Add(Errors.CLOUD_USER_ALTER_NOT_ALLOWED, "");

            // Prevent deletion of SuperAdmins if you aren't one
            if (user.HasRole(Models.User.Role.SuperAdmin) && !CurrentUser().HasRole(Models.User.Role.SuperAdmin))
                _response.Errors.Add(Errors.ROLE_VALIDATION_FAILED, "");

            // Prevent deletion of WebApi users if you aren't a SuperAdmin
            if (user.HasRole(Models.User.Role.WebApi) && !CurrentUser().HasRole(Models.User.Role.SuperAdmin))
                _response.Errors.Add(Errors.ROLE_VALIDATION_FAILED, "");

            // Prevent removing own account
            if (user.AuthKey.Equals(CurrentUser().AuthKey))
                _response.Errors.Add(Errors.USER_CANNOT_REMOVE_SELF, "");

            if (HasErrors())
                return StatusCode(403, _response);

            // Is this user a SuperAdmin? Removing him should thus require that at least one OTHER SuperAdmin is defined.
            // i.e: 1+1 = 2 should be the minimal limit
            if (user.HasRole(Models.User.Role.SuperAdmin))
                await EnsureRoleCount(Models.User.Role.SuperAdmin, 2);
            
            // Actually remove references
            ClearUserFromCacheIfExists(user.AuthKey);
            await Db.DeleteByIdAsync<User>(user.Id);
            
            return NoContent();

        }

        // Used to invalidate a cached user if they are deleted / updated
        private void ClearUserFromCacheIfExists(string username)
        {
            AuthUtils.ClearUserFromCacheIfExists(_cache, username);
        }

        // TODO: Look into whether making this a responsibility of the service itself (generation) is a more sane appraoch
        // This would foster loose coupling
        private async Task<UserServiceResource> GenerateUserServiceResource(User user, Type type, IEnumerable<IServiceConfig> configs)
        {
            var serviceReference = new UserServiceResource();
            
            EnsureServiceAccess(user, type);
            
            // Giant hack, but hey, it works ┐(´∀｀)┌ﾔﾚﾔﾚ
            switch (true)
            {
                case bool _ when type == typeof(HTTPProxy):
                    // HTTPProxy has a global config, and thus only one instance
                    // Guaranteed to not be null, so inspection is not needed.
                    // ReSharper disable once AssignNullToNotNullAttribute
                    var config = (HTTPConfig) configs.First();
                    var proxies = new List<string>();
                    foreach (var listener in config.listeners)
                    {
                        // Gotta translate 0.0.0.0 into something people can actually connect to
                        // This is currently replaced by the default outgoing IP, which should work assuming NAT port forwarding succeeded
                        // Or there is no NAT involved.
                        proxies.Add(await _ipResolver.Translate(listener.Item1) + ":" + listener.Item2);
                    }

                    serviceReference.AccessReference = proxies;
                    serviceReference.AccessCredentials = Messages.SPECTERO_USERNAME_PASSWORD;
                    break;
                case bool _ when type == typeof(OpenVPN):
                    // OpenVPN is a multi-instance service
                    var allListeners = new List<OpenVPNListener>();
                    
                    OpenVPNConfig sanitizedOpenVPNConfig = null;

                    foreach (var vpnConfig in configs)
                    {
                        var castConfig = vpnConfig as OpenVPNConfig;
                        if (castConfig?.Listener != null)
                        {
                            // ReSharper disable once InconsistentNaming
                            var translatedIP = await _ipResolver.Translate(castConfig.Listener.IPAddress);

                            // This is done to force a translation of local addresses
                            castConfig.Listener.IPAddress = translatedIP.ToString();

                            allListeners.Add(castConfig.Listener);

                            // Setting it only once would be ideal, but eh -- overhead is low enough to make this work.
                            sanitizedOpenVPNConfig = castConfig;
                        }
                    }

                    if (!allListeners.IsNullOrEmpty())
                    {
                        serviceReference.AccessConfig = await _razorLightEngine.CompileRenderAsync("OpenVPNUser", new OpenVPNUserConfig
                        {
                            Listeners = allListeners,
                            User = user,
                            Identity = _identityProvider.GetGuid().ToString(),
                            BaseConfig = sanitizedOpenVPNConfig
                        });

                        serviceReference.AccessCredentials = user?.CertKey;
                    }

                    break;
            }

            return serviceReference;
        }

        [HttpGet("{id}/service-resources/{name?}")]
        public async Task<IActionResult> GetUserServiceSetupResources(int id, string name = "")
        {
            var user = await Db.SingleByIdAsync<User>(id);
            if (user == null)
                _response.Errors.Add(Errors.USER_NOT_FOUND, "");

            if (HasErrors())
                return BadRequest(_response);

            if (Defaults.ValidServices.Any(s => s == name) || name.IsEmpty())
            {
                if (!name.IsEmpty())
                {
                    var type = Utility.GetServiceType(name);

                    EnsureServiceAccess(user, type);
                    
                    var configs = _serviceConfigManager.Generate(type);
                    _response.Result = await GenerateUserServiceResource(user, type, configs);
                }
                else
                {
                    var resultDictionary = new Dictionary<string, UserServiceResource>();
                    foreach (var serviceName in Defaults.ValidServices)
                    {
                        var type = Utility.GetServiceType(serviceName);
                        
                        // TODO: Fix this constraint once the other services are implemented.
                        // The config manager's dictionary cannot lookup a null value, this fixes that (since GetServiceType returns null for ShadowSOCKS/SSHTunnel)
                        if (type != null && EnsureServiceAccess(user, type, false))
                        {
                            var configs = _serviceConfigManager.Generate(type);
                            resultDictionary.Add(serviceName,
                                await GenerateUserServiceResource(user, type, configs));
                        }
                    }

                    _response.Result = resultDictionary;
                }
            }

            if (_response.Result != null)
                return Ok(_response);

            _response.Errors.Add(Errors.INVALID_SERVICE_OR_ACTION_ATTEMPT, "");
            return BadRequest(_response);
        }

        private static bool EnsureServiceAccess(User user, Type serviceName, bool throwsExceptions = true)
        {
            var fail = false;
            
            switch (true)
            {
                case bool _ when serviceName == typeof(HTTPProxy):
                    if (!user.Can(Models.User.Action.ConnectToHTTPProxy))
                        fail = true;
                    
                    break;
                
                case bool _ when serviceName == typeof(OpenVPN):
                    if (!user.Can(Models.User.Action.ConnectToOpenVPN))
                        fail = true;
                    
                    break;
                
                default:
                    if (throwsExceptions)
                        throw new DisclosableError(Errors.INVALID_SERVICE_OR_ACTION_ATTEMPT);
                    else
                        fail = true;
                    break;
            }

            if (fail && throwsExceptions)
                throw new DisclosableError(why: Errors.SERVICE_ACCESS_DENIED, code: HttpStatusCode.Forbidden);
            
            // The purpose is to "ensure success," i.e: we need to return true on success (and false on failure).
            // This, inversion.
            
            return ! fail;
        }
    }
}