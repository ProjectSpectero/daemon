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

namespace Spectero.daemon.Controllers
{
    public class AuthController : BaseController
    {
        private readonly ICryptoService _cryptoService;
        public AuthController(IOptionsSnapshot<AppConfig> appConfig, ILogger<AuthController> logger,
            IDbConnection db, ICryptoService cryptoService)
            : base(appConfig, logger, db)
        {
            _cryptoService = cryptoService;
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> AuthenticateUser([FromBody] TokenRequest request)
        {
            string username = request.Username;
            string password = request.Password;

            if (username.IsNullOrEmpty() || password.IsNullOrEmpty())
                _response.Errors.Add(Errors.MISSING_BODY);

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
                        issuer: "yourdomain.com",
                        audience: "yourdomain.com",
                        claims: claims,
                        expires: DateTime.Now.AddMinutes(AppConfig.JWTTokenExpiryInMinutes),
                        signingCredentials: credentials
                    );
                _response.Result = token;
                return Ok(_response);
            }

            _response.Errors.Add(Errors.AUTHENTICATION_FAILED); // Won't disclose why it failed, for that is a security risk
            return StatusCode(403, _response);
        }
    }
}