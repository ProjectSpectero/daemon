using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
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

            try
            {
                userId = await Db.InsertAsync<User>(user, selectIdentity: true);
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

            return Created(Url.RouteUrl("GetUserById", new { id = userId }), _response); // See why 201 - Created() doesn't work here

        }

        [HttpGet("{id}", Name = "GetUserById")]
        public async Task<IActionResult> GetUserById(long id)
        {
            var user = await Db.SingleByIdAsync<User>(id);
            if (user != null)
            {
                _response.Result = new { Id = user.Id, AuthKey = user.AuthKey, Cert = user.Cert, CreatedDate = user.CreatedDate }; // Hide password and certkey
                return Ok(_response);
            }
            else
                return NotFound(_response);
        }

        [HttpGet("", Name = "GetUsers")]
        public async Task<IActionResult> GetUsers()
        {
            var users = await Db.SelectAsync<User>();
            var ret = new List<User>();
            foreach (var user in users)
            {
                // Abstract Password and Certkey
                ret.Add(new User
                {
                    Id = user.Id,
                    AuthKey = user.AuthKey,
                    Cert = user.Cert,
                    CreatedDate = user.CreatedDate
                });
            }
            _response.Result = ret;
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

        // GET
        public IActionResult Index()
        {
            return Ok();
        }
    }
}