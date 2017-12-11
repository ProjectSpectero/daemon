using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using ServiceStack.DataAnnotations;

namespace Spectero.daemon.Models
{
    public class User : IModel
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



        [Index]
        [AutoIncrement]
        public long Id { get; set; }

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

        [JsonProperty("password")]
        [Ignore]
        public string PasswordSetter { set => Password = value; }

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

        [DataType(DataType.Date)]
        public DateTime CreatedDate{ get; set; }

        [DataType(DataType.Date)]
        public DateTime LastLoginTime { get; set; }

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
    }
}