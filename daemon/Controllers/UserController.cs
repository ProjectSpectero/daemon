using System;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ServiceStack;
using ServiceStack.OrmLite;
using Spectero.daemon.Libraries.Config;
using Spectero.daemon.Libraries.Core;
using Spectero.daemon.Libraries.Core.Constants;
using Spectero.daemon.Models;
using Messages = Spectero.daemon.Libraries.Core.Constants.Messages;

namespace Spectero.daemon.Controllers
{
    [Microsoft.AspNetCore.Mvc.Route("v1/[controller]")]
    [ApiExplorerSettings(IgnoreApi = false, GroupName = nameof(UserController))]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public class UserController : BaseController
    {
        private readonly IMemoryCache _cache;
        private readonly Regex _userRegex;

        public UserController(IOptionsSnapshot<AppConfig> appConfig, ILogger<UserController> logger,
            IDbConnection db, IMemoryCache cache)
            : base(appConfig, logger, db)
        {
            _cache = cache;
            _userRegex = new Regex(@"^[a-zA-Z][\w]*$");
        }

        [HttpPost("", Name = "CreateUser")]
        public async Task<IActionResult> Create ([FromBody] User user)
        {
            if (ModelState.IsValid)
            {
                if (!user.Validate(out var validationErrors))
                    _response.Errors.Add(Errors.VALIDATION_FAILED, validationErrors);
            }           
            else
                _response.Errors.Add(Errors.MISSING_BODY, "");

            if (HasErrors())
                return BadRequest(_response);

            if ((user.HasRole(Models.User.Role.SuperAdmin) ||
                 user.HasRole(Models.User.Role.WebApi)) && ! CurrentUser().HasRole(Models.User.Role.SuperAdmin))
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

            // TODO: Generate a cert and a certkey when creating user

            try
            {
                userId = await Db.InsertAsync<User>(user, true);
            }
            catch (DbException e)
            {
                Logger.LogError(e.Message);
                _response.Errors.Add(Errors.OBJECT_PERSIST_FAILED, e.Message); // Poor man's fluent validation, fix later. Here's to hoping DB validation actually works.
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
                // Prevent deletion of cloud users
                if (user.Source.Equals(Models.User.SourceTypes.SpecteroCloud))
                    _response.Errors.Add(Errors.CLOUD_USER_ALTER_NOT_ALLOWED, "");

                // Prevent deletion of SuperAdmins if you aren't one
                if (user.HasRole(Models.User.Role.SuperAdmin) && ! CurrentUser().HasRole(Models.User.Role.SuperAdmin))
                    _response.Errors.Add(Errors.ROLE_VALIDATION_FAILED, "");

                // Prevent deletion of WebApi users if you aren't a SuperAdmin
                if (user.HasRole(Models.User.Role.WebApi) && ! CurrentUser().HasRole(Models.User.Role.SuperAdmin))
                    _response.Errors.Add(Errors.ROLE_VALIDATION_FAILED, "");

                // Prevent removing own account
                if (user.AuthKey.Equals(CurrentUser().AuthKey))
                    _response.Errors.Add(Errors.USER_CANNOT_REMOVE_SELF, "");

                if (HasErrors())
                    return StatusCode(403, _response);

                ClearUserFromCacheIfExists(user.AuthKey);
                await Db.DeleteByIdAsync<User>(user.Id);
                return NoContent();
            }
            else
                return NotFound(_response);
        }

        [HttpPut("{id}", Name = "UpdateUser")]
        public async Task<IActionResult> UpdateUser(long id, [FromBody] User user)
        {
            if (ModelState.IsValid)
            {
                if (!user.Validate(out var validationErrors))
                    _response.Errors.Add(Errors.VALIDATION_FAILED, validationErrors);
            }
            else
                _response.Errors.Add(Errors.MISSING_BODY, "");

            if (HasErrors())
                return BadRequest(_response);

            User fetchedUser = null;

            fetchedUser = await Db.SingleByIdAsync<User>(id);

            if (fetchedUser == null)
            {
                _response.Errors.Add(Errors.USER_NOT_FOUND, "");
                return StatusCode(404, _response);
            }

            if (fetchedUser.Source.Equals(Models.User.SourceTypes.SpecteroCloud))
            {
                _response.Errors.Add(Errors.CLOUD_USER_ALTER_NOT_ALLOWED, "");
                return StatusCode(403, _response);
            }

            // Not allowed to edit an existing superadmin if you aren't one
            if (fetchedUser.HasRole(Models.User.Role.SuperAdmin) &&
                !CurrentUser().HasRole(Models.User.Role.SuperAdmin))
            {
                _response.Errors.Add(Errors.ROLE_VALIDATION_FAILED, "");
                return StatusCode(403, _response);
            }

            if (!user.AuthKey.IsNullOrEmpty() && !fetchedUser.AuthKey.Equals(user.AuthKey))
            {
                if (!user.AuthKey.ToLower().Equals(user.AuthKey))
                {
                    user.AuthKey = user.AuthKey.ToLower();
                    _response.Message = Messages.USER_AUTHKEY_FLATTENED;
                }
                fetchedUser.AuthKey = user.AuthKey;
            }
                
            if (!user.Password.IsNullOrEmpty())
                fetchedUser.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);

            if (!user.FullName.IsNullOrEmpty())
                fetchedUser.FullName = user.FullName;

            if (!user.EmailAddress.IsNullOrEmpty())
                fetchedUser.EmailAddress = user.EmailAddress;

            if (!user.Roles.SequenceEqual(fetchedUser.Roles))
            {
                // No need to care about roles unless they're changing
                Logger.LogDebug("UU: Datastore roles and requested roles are different.");

                if (user.HasRole(Models.User.Role.WebApi) && (!fetchedUser.HasRole(Models.User.Role.WebApi) &&
                                                              !CurrentUser().HasRole(Models.User.Role.SuperAdmin)))
                    _response.Errors.Add(Errors.ROLE_VALIDATION_FAILED, "");

                if (user.HasRole(Models.User.Role.SuperAdmin) &&
                    (!fetchedUser.HasRole(Models.User.Role.SuperAdmin) &&
                     !CurrentUser().HasRole(Models.User.Role.SuperAdmin)))
                    _response.Errors.Add(Errors.ROLE_VALIDATION_FAILED, "");

                if (HasErrors())
                    return StatusCode(403, _response);

                // After verifying, assign the new roles for DB commit
                fetchedUser.Roles = user.Roles;
            }

            try
            {
                await Db.UpdateAsync(fetchedUser);
            }
            catch (DbException e)
            {
                Logger.LogError(e.Message);
                _response.Errors.Add(Errors.VALIDATION_FAILED, e.Message); // Poor man's fluent validation, fix later. Here's to hoping DB validation actually works.
            }
            
            if (HasErrors())
                return BadRequest(_response);

            _response.Result = fetchedUser;

            ClearUserFromCacheIfExists(fetchedUser.AuthKey);
            return Ok(_response);
        }

        // Used to invalidate a cached user if they are deleted / updated
        private void ClearUserFromCacheIfExists(string username)
        {
            var key = Utility.GenerateCacheKey(username);
            if (_cache.Get<User>(key) != null)
            {
                _cache.Remove(key);
            }
        }
    }
}