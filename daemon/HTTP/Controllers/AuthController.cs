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
using Spectero.daemon.Libraries.Core.Identity;
using Spectero.daemon.Models.Opaque;
using Spectero.daemon.Models.Opaque.Requests;
using Spectero.daemon.Models.Opaque.Responses;
using Messages = Spectero.daemon.Libraries.Core.Constants.Messages;

namespace Spectero.daemon.HTTP.Controllers
{
    [Route("v1/[controller]")]
    [ApiExplorerSettings(IgnoreApi = false, GroupName = nameof(AuthController))]
    public class AuthController : BaseController
    {
        private readonly ICryptoService _cryptoService;
        private readonly IAuthenticator _authenticator;
        private readonly IIdentityProvider _identityProvider;

        public AuthController(IOptionsSnapshot<AppConfig> appConfig, ILogger<AuthController> logger,
            IDbConnection db, ICryptoService cryptoService,
            IAuthenticator authenticator, IIdentityProvider identityProvider)
            : base(appConfig, logger, db)
        {
            _authenticator = authenticator;
            _cryptoService = cryptoService;
            _identityProvider = identityProvider;
        }

        private async Task<IActionResult> ScopeAwareAuthentication(string username, string password, Models.User.Action scope)
        {
            var user = await _authenticator.Authenticate(username, password, scope);
            if (user == null)
                _response.Errors.Add(Errors.AUTHENTICATION_FAILED, "");

            if (HasErrors()) return StatusCode(403, _response);

            _response.Message = Messages.AUTHENTICATION_SUCCEEDED;

            switch (scope)
            {
                case Models.User.Action.ManageApi:
                case Models.User.Action.ManageDaemon:

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
                    };

                    var key = _cryptoService.GetJWTSigningKey();
                    var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha512); // Hardcoded alg for now, perhaps allow changing later

                    var accessExpires =
                        DateTime.Now.AddMinutes(AppConfig.JWTTokenExpiryInMinutes > 0
                            ? AppConfig.JWTTokenExpiryInMinutes
                            : 60); // 60 minutes by default

                    var token = new JwtSecurityToken
                    (
                        // Can't issue aud/iss since we have no idea what the accessing URL will be.
                        // This is not a typical webapp with static `Host`
                        claims: claims,
                        expires: accessExpires,
                        signingCredentials: credentials,
                        issuer: _identityProvider.GetFQDN()
                    );

                    var accessToken = new Token
                    {
                        token = new JwtSecurityTokenHandler().WriteToken(token),
                        expires = ((DateTimeOffset)accessExpires).ToUnixTimeSeconds()
                    };

                    var refreshExpires =
                        accessExpires.AddMinutes(AppConfig.JWTRefreshTokenDelta > 0 ? AppConfig.JWTRefreshTokenDelta : 30);

                    var refreshToken = new Token
                    {
                        token = null,
                        expires = ((DateTimeOffset)refreshExpires).ToUnixTimeSeconds()
                    };

                    _response.Message = Messages.JWT_TOKEN_ISSUED;
                    _response.Result = new AuthResponse
                    {
                        Access = accessToken,
                        Refresh = refreshToken
                    };
                    break;
            }

            return Ok(_response);
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

            Models.User.Action scope;

            switch (request.ServiceScope)
            {
                case "HTTPProxy":
                    scope = Models.User.Action.ConnectToHTTPProxy;
                    break;

                case "OpenVPN":
                    scope = Models.User.Action.ConnectToOpenVPN;
                    break;

                case "SSHTunnel":
                    scope = Models.User.Action.ConnectToSSHTunnel;
                    break;

                case "ShadowSOCKS":
                    scope = Models.User.Action.ConnectToShadowSOCKS;
                    break;

                default:
                    scope = Models.User.Action.ManageApi;
                    break;
            }

            return await ScopeAwareAuthentication(request.AuthKey, request.Password, scope);
        }
    }
}