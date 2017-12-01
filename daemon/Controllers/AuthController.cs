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
using ServiceStack;
using ServiceStack.OrmLite;
using Spectero.daemon.Libraries.Config;
using Spectero.daemon.Libraries.Core.Constants;
using Spectero.daemon.Libraries.Core.Crypto;
using Spectero.daemon.Models;
using Messages = Spectero.daemon.Libraries.Core.Constants.Messages;

namespace Spectero.daemon.Controllers
{
    [Microsoft.AspNetCore.Mvc.Route("v1/[controller]")]
    public class AuthController : BaseController
    {
        private readonly ICryptoService _cryptoService;
        public AuthController(IOptionsSnapshot<AppConfig> appConfig, ILogger<AuthController> logger,
            IDbConnection db, ICryptoService cryptoService)
            : base(appConfig, logger, db)
        {
            _cryptoService = cryptoService;
        }

        [HttpPost("", Name = "RequestJWTToken")]
        [AllowAnonymous]
        public async Task<IActionResult> AuthenticateUser([FromBody] TokenRequest request)
        {
            if (! ModelState.IsValid || request.AuthKey.IsNullOrEmpty() || request.Password.IsNullOrEmpty())
                _response.Errors.Add(Errors.MISSING_BODY);

            if (HasErrors()) return StatusCode(403, _response);

            string username = request.AuthKey;
            string password = request.Password;

            var user = await Db.SingleAsync<User>(x => x.AuthKey == username);
                
            if (user != null && BCrypt.Net.BCrypt.Verify(password, user.Password))
            {
                var claims = new[]
                {
                    new Claim(ClaimTypes.UserData, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.AuthKey)
                    // Todo: Add roles
                };
                var key = _cryptoService.GetJWTSigningKey();
                var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256); // Hardcoded alg for now, perhaps allow changing later
                var token = new JwtSecurityToken
                    (
                        // Can't issue aud/iss since we have no idea what the accessing URL will be. This is not a typical webapp with static `Host`
                        claims: claims,
                        expires: DateTime.Now.AddMinutes(AppConfig.JWTTokenExpiryInMinutes > 0 ? AppConfig.JWTTokenExpiryInMinutes : 60),
                        signingCredentials: credentials
                    );
                _response.Message = Messages.JWT_TOKEN_ISSUED;
                _response.Result = new JwtSecurityTokenHandler().WriteToken(token);
                return Ok(_response);
            }

            _response.Errors.Add(Errors.AUTHENTICATION_FAILED); // Won't disclose why it failed, for that is a security risk
            return StatusCode(403, _response);
        }
    }
}