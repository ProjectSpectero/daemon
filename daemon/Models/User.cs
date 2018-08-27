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
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using ServiceStack;
using ServiceStack.DataAnnotations;
using Spectero.daemon.Libraries.Config;
using Spectero.daemon.Libraries.Core.Constants;
using Valit;

namespace Spectero.daemon.Models
{
    public class User : BaseModel
    {
        [EnumAsInt]
        public enum SourceTypes
        {
            Local,
            SpecteroCloud
        }

        [EnumAsInt]
        public enum Role
        {
            SuperAdmin,
            WebApi,
            HTTPProxy,
            OpenVPN,
            ShadowSOCKS,
            SSHTunnel
        }

        public enum Action
        {
            ManageDaemon,
            ManageApi,
            ConnectToHTTPProxy,
            ConnectToOpenVPN,
            ConnectToShadowSOCKS,
            ConnectToSSHTunnel
        }

        [ServiceStack.DataAnnotations.Required]
        [ServiceStack.DataAnnotations.StringLength(50)]
        [Index(Unique = true)]
        public string AuthKey { get; set; }
        
        [JsonConverter(typeof(StringEnumConverter))]
        public SourceTypes Source { get; set; }

        [JsonProperty("roles", ItemConverterType = typeof(StringEnumConverter))]
        public List<Role> Roles { get; set; }

        // Certificate Information
        public string Cert { get; set; }
        public string CertKey { get; set; }
        
        [Ignore]
        public bool? EncryptCertificate { get; set; }
        
        public long EngagementId { get; set; }
        
        // User Information
        public string FullName { get; set; }
        public string EmailAddress { get; set; }
        
        [Ignore]
        private string RawPassword { get; set; }

        [JsonProperty("password")]
        [Ignore]
        public string PasswordSetter
        {
            set
            {
                if (!value.IsEmpty())
                {
                    RawPassword = value;
                    Password = BCrypt.Net.BCrypt.HashPassword(value);
                }
            }  
        }
        
        [ServiceStack.DataAnnotations.Required]
        [JsonIgnore] // Prevent JSON serialization
        public string Password { get; set; }
        
        [DataType(DataType.Date)]
        public DateTime LastLoginDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime CloudSyncDate { get; set; }

        /// <summary>
        /// Convert the user data to a interpolated string.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"Id -> {Id}, Authkey -> {AuthKey}, Password -> {Password}, Created At -> {CreatedDate}";
        }

        /// <summary>
        /// Getter to determine if the user has a specified role
        /// </summary>
        /// <param name="role"></param>
        /// <returns></returns>
        internal bool HasRole(Role role)
        {
            if (Roles == null || Roles.Count == 0)
                return false;

            return Roles.Contains(role);
        }

        /// <summary>
        /// Determines if a user can perform an action.
        /// Poor man's RBAC, our needs are not big enough to use a proper roles framework.
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        internal bool Can(Action action)
        {
            if (HasRole(Role.SuperAdmin))
                return true;

            if (HasRole(Role.WebApi))
            {
                if (action != Action.ManageDaemon)
                    return true;
                return false;
            }

            if (HasRole(Role.HTTPProxy) && action.Equals(Action.ConnectToHTTPProxy))
                return true;

            if (HasRole(Role.OpenVPN) && action.Equals(Action.ConnectToOpenVPN))
                return true;

            if (HasRole(Role.ShadowSOCKS) && action.Equals(Action.ConnectToShadowSOCKS))
                return true;

            if (HasRole(Role.SSHTunnel) && action.Equals(Action.ConnectToSSHTunnel))
                return true;

            return false;
        }


        /// <summary>
        /// Built in model object validator for posting information.
        /// </summary>
        /// <param name="errors"></param>
        /// <param name="operation"></param>
        /// <returns></returns>
        public override bool Validate(out ImmutableArray<string> errors, CRUDOperation operation = CRUDOperation.Create)
        {
            IValitResult result = ValitRules<User>
                .Create()
                .Ensure(m => m.AuthKey, _ => _
                    .Required()
                        .WithMessage(FormatValidationError(Errors.FIELD_REQUIRED, "authKey"))
                    .MaxLength(50)
                        .WithMessage(FormatValidationError(Errors.FIELD_MAXLENGTH, "authKey", "50"))
                    .Matches(@"^[a-zA-Z][\w]*$")
                        .WithMessage(FormatValidationError(Errors.FIELD_REGEX_MATCH, "authKey", @"^[a-zA-Z][\w]*$")))
                .Ensure(m => m.AuthKey, _ => _
                    .Satisfies(m => ! m.IsNullOrEmpty() && ! m.Equals(AppConfig.CloudConnectDefaultAuthKey))
                        .WithMessage(FormatValidationError(Errors.FIELD_RESERVED, "authKey")))
                .Ensure(m => m.RawPassword, _ => _
                    .Required()
                        .WithMessage(FormatValidationError(Errors.FIELD_REQUIRED, "password"))
                        .When(m => operation.Equals(CRUDOperation.Create) || operation.Equals(CRUDOperation.Update) && !m.RawPassword.IsNullOrEmpty())
                    .MinLength(5)
                        .WithMessage(FormatValidationError(Errors.FIELD_MINLENGTH, "password", "5"))
                        .When(m => operation.Equals(CRUDOperation.Create) || operation.Equals(CRUDOperation.Update) && !m.RawPassword.IsNullOrEmpty())
                    .MaxLength(72)
                        .WithMessage(FormatValidationError(Errors.FIELD_MAXLENGTH, "password", "72"))
                        .When(m => operation.Equals(CRUDOperation.Create) || operation.Equals(CRUDOperation.Update) && !m.RawPassword.IsNullOrEmpty()))
                .Ensure(m => m.FullName, _ => _
                    .MaxLength(50)
                        .WithMessage(FormatValidationError(Errors.FIELD_MAXLENGTH, "fullName", "50"))
                        .When(m => ! m.FullName.IsNullOrEmpty()))
                .Ensure(m => m.EmailAddress, _ => _
                    .Required()
                        .WithMessage(FormatValidationError(Errors.FIELD_REQUIRED, "email"))
                    .Email()
                        .WithMessage(FormatValidationError(Errors.FIELD_EMAIL, "email")))
                .Ensure(m => m.EncryptCertificate, _ => _
                    .Required()
                        .WithMessage(FormatValidationError(Errors.FIELD_REQUIRED, "encryptCertificate"))
                        .When(m => operation.Equals(CRUDOperation.Create)))
                .For(this)
                .Validate();

            errors = result.ErrorMessages;
            return result.Succeeded;
        }

    }
}