using System;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Spectero.daemon.Libraries.Config;
using Spectero.daemon.Libraries.Core.Authenticator;
using Spectero.daemon.Libraries.Core.Constants;
using Spectero.daemon.Libraries.Core.Crypto;
using Spectero.daemon.Models.Requests;
using Messages = Spectero.daemon.Libraries.Core.Constants.Messages;

namespace Spectero.daemon.Controllers
{
    [Route("v1/[controller]")]
    [ApiExplorerSettings(IgnoreApi = false, GroupName = nameof(AuthController))]
    public class AuthController : BaseController
    {
        private readonly ICryptoService _cryptoService;
        private readonly IAuthenticator _authenticator;

        public AuthController(IOptionsSnapshot<AppConfig> appConfig, ILogger<AuthController> logger,
            IDbConnection db, ICryptoService cryptoService,
            IAuthenticator authenticator)
            : base(appConfig, logger, db)
        {
            _authenticator = authenticator;
            _cryptoService = cryptoService;
        }

        [HttpPost("", Name = "RequestJWTToken")]
        [AllowAnonymous]
        public async Task<IActionResult> AuthenticateUser([FromBody] TokenRequest request)
        {
            if (ModelState.IsValid)
            {
                if (!request.Validate(out var validationErrors))
                    _response.Errors.Add(Errors.VALIDATION_FAILED, validationErrors);
            }
            else
                _response.Errors.Add(Errors.MISSING_BODY, "");
            

            if (HasErrors()) return StatusCode(403, _response);

            var username = request.AuthKey;
            var password = request.Password;
            var user = await _authenticator.Authenticate(username, password, Models.User.Action.ManageApi);

            if (user != null)
            {
                // Intentionally hidden to keep the JWT length manageable
                user.Cert = null;
                user.CertKey = null;

                var userJson = JsonConvert.SerializeObject(user,
                    new JsonSerializerSettings
                    {
                        ContractResolver = new CamelCasePropertyNamesContractResolver()
                    }
                );

                var claims = new[]
                {
                    new Claim(ClaimTypes.UserData, userJson),
                    // Todo: Add roles
                };
                var key = _cryptoService.GetJWTSigningKey();
                var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256); // Hardcoded alg for now, perhaps allow changing later
                var token = new JwtSecurityToken
                (
                    // Can't issue aud/iss since we have no idea what the accessing URL will be.
                    // This is not a typical webapp with static `Host`
                    claims: claims,
                    expires: DateTime.Now.AddMinutes(AppConfig.JWTTokenExpiryInMinutes > 0 ? AppConfig.JWTTokenExpiryInMinutes : 60), // 60 minutes by default
                    signingCredentials: credentials
                );
                _response.Message = Messages.JWT_TOKEN_ISSUED;
                _response.Result = new JwtSecurityTokenHandler().WriteToken(token);
                return Ok(_response);
            }
                
            _response.Errors.Add(Errors.AUTHENTICATION_FAILED, ""); // Won't disclose why it failed, for that is a security risk
            return StatusCode(403, _response);
        }
    }
}