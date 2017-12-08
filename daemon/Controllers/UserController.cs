using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ServiceStack;
using ServiceStack.OrmLite;
using Spectero.daemon.Libraries.Config;
using Spectero.daemon.Libraries.Core.Constants;
using Spectero.daemon.Models;

namespace Spectero.daemon.Controllers
{
    [Microsoft.AspNetCore.Mvc.Route("v1/[controller]")]
    [ApiExplorerSettings(IgnoreApi = false, GroupName = nameof(UserController))]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public class UserController : BaseController
    {
        public UserController(IOptionsSnapshot<AppConfig> appConfig, ILogger<UserController> logger,
            IDbConnection db)
            : base(appConfig, logger, db)
        {

        }

        [HttpPost("", Name = "CreateUser")]
        public async Task<IActionResult> Create ([FromBody] User user)
        {
            try
            {
                if (!user.Password.IsNullOrEmpty())
                    user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);
                else
                    _response.Errors.Add(Errors.MISSING_OR_INVALID_PASSWORD);
            }
            catch (NullReferenceException e)
            {
                Logger.LogError(e.Message);
                _response.Errors.Add(Errors.MISSING_BODY);
            }
          
            if (HasErrors())
                return BadRequest(_response);

            long userId = -1;
            user.CreatedDate = DateTime.Now;
            user.Source = Models.User.SourceTypes.Local;

            try
            {
                userId = await Db.InsertAsync<User>(user, true);
            }
            catch (DbException e)
            {
                Logger.LogError(e.Message);
                _response.Errors.Add(e.Message); // Poor man's fluent validation, fix later. Here's to hoping DB validation actually works.
            }

            if (HasErrors())
                return BadRequest(_response);

            user.Id = userId;
            _response.Result = user;

            return Created(Url.RouteUrl("GetUserById", new { id = userId }), _response);

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

        [HttpDelete("{id}", Name = "DeleteUser")]
        public async Task<IActionResult> DeleteUser(long id)
        {
            var user = await Db.SingleByIdAsync<User>(id);
            if (user != null)
            {
                await Db.DeleteAsync<User>(user);
                return NoContent();
            }
            else
                return NotFound(_response);

        }

        [HttpPut("", Name = "UpdateUser")]
        public async Task<IActionResult> UpdateUser([FromBody] User user)
        {
            long Id;
            string AuthKey;
            string Password;
            string Cert;
            string CertKey;

            User fetchedUser = null;

            try
            {
                fetchedUser = await Db.SingleByIdAsync<User>(user.Id);

                if (fetchedUser == null)
                {
                    _response.Errors.Add(Errors.MISSING_BODY);
                    _response.Errors.Add(Errors.USER_NOT_FOUND);
                    return BadRequest(_response);
                }

                if (!user.AuthKey.IsNullOrEmpty() && !fetchedUser.AuthKey.Equals(user.AuthKey))
                    fetchedUser.AuthKey = user.AuthKey;

                if (!user.Password.IsNullOrEmpty())
                    fetchedUser.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);

                // TODO: Actually validate cert and certkey

                if (!user.CertKey.IsNullOrEmpty() && !fetchedUser.CertKey.Equals(user.CertKey))
                    fetchedUser.CertKey = user.CertKey;

                if (!user.Cert.IsNullOrEmpty() && !fetchedUser.Cert.Equals(user.Cert))
                    fetchedUser.Cert = user.Cert;
            }
            catch (NullReferenceException e)
            {
                Logger.LogError(e.Message);
                _response.Errors.Add(Errors.MISSING_BODY);
            }

            if (fetchedUser == null || HasErrors())
                return BadRequest(_response);

            try
            {
                await Db.UpdateAsync(fetchedUser);
            }
            catch (DbException e)
            {
                Logger.LogError(e.Message);
                _response.Errors.Add(e.Message); // Poor man's fluent validation, fix later. Here's to hoping DB validation actually works.
            }
            
            if (HasErrors())
                return BadRequest(_response);

            _response.Result = fetchedUser;

            return Ok(_response);
        }

        // GET
        public IActionResult Index()
        {
            return Ok();
        }
    }
}