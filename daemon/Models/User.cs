using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using ServiceStack;
using ServiceStack.DataAnnotations;
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

        [JsonConverter(typeof(StringEnumConverter))]
        public SourceTypes Source { get; set; }

        [ServiceStack.DataAnnotations.Required]
        [ServiceStack.DataAnnotations.StringLength(50)]
        [Index(Unique = true)]
        public string AuthKey { get; set; }

        [JsonProperty("roles", ItemConverterType = typeof(StringEnumConverter))]
        public List<Role> Roles { get; set; }


        [ServiceStack.DataAnnotations.Required]
        [JsonIgnore] // Prevent JSON serialization
        public string Password { get; set; }

        [Ignore]
        private string RawPassword { get; set; }

        [JsonProperty("password")]
        [Ignore]
        public string PasswordSetter
        {
            set
            {
                RawPassword = value;
                Password = BCrypt.Net.BCrypt.HashPassword(value);
            }  
        }

        public string Cert { get; set; }

        [JsonIgnore] // Prevent JSON serialization
        public string CertKey { get; set; }

        [JsonProperty("certKey")]
        [Ignore]
        public string CertKeySetter
        {
            set => CertKey = value;
        }

        public long SpecteroEngagementId = 0;

        public string FullName { get; set; }
        public string EmailAddress { get; set; }

        [DataType(DataType.Date)]
        public DateTime LastLoginDate { get; set; }

        public override string ToString()
        {
            return "Id -> " + Id + ", AuthKey -> " + AuthKey + ", Password -> " + Password + ", Created At -> " + CreatedDate;
        }

        internal bool HasRole(Role role)
        {
            if (Roles == null || Roles.Count == 0)
                return false;

            return Roles.Contains(role);
        }

        /*
         * Poor man's RBAC, our needs are not big enough to use a proper roles framework.
         */
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

        /*
         * Built in object validator
         */

        public override bool Validate(out ImmutableArray<string> errors)
        {
            IValitResult result = ValitRules<User>
                .Create()
                .Ensure(m => m.AuthKey, _ => _
                    .Required()
                        .WithMessage(FormatValidationError(Errors.FIELD_REQUIRED, "authKey"))
                    .MaxLength(50)
                        .WithMessage(FormatValidationError(Errors.FIELD_MAXLENGTH, "authKey"))
                    .Matches(@"^[a-zA-Z][\w]*$")
                        .WithMessage(FormatValidationError(Errors.FIELD_REGEX_MATCH, "authKey", @"^[a-zA-Z][\w]*$")))
                .Ensure(m => m.RawPassword, _ => _
                    .MinLength(5)
                        .WithMessage(FormatValidationError(Errors.FIELD_MINLENGTH, "password", "5"))
                        .When(m => !m.RawPassword.IsNullOrEmpty())
                    .MaxLength(72)
                        .WithMessage(FormatValidationError(Errors.FIELD_MAXLENGTH, "password", "72"))
                        .When(m => !m.RawPassword.IsNullOrEmpty()))
                .Ensure(m => m.FullName, _ => _
                    .MaxLength(50)
                        .WithMessage(FormatValidationError(Errors.FIELD_MAXLENGTH, "fullName", "50"))
                        .When(m => ! m.FullName.IsNullOrEmpty()))
                .Ensure(m => m.EmailAddress, _ => _
                    .Email()
                    .WithMessage(FormatValidationError(Errors.FIELD_EMAIL, "email"))
                    .When(m => ! m.EmailAddress.IsNullOrEmpty())
                )
                .For(this)
                .Validate();

            errors = result.ErrorMessages;
            return result.Succeeded;
        }

    }
}