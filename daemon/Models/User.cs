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
            HttpProxy,
            OpenVPN,
            ShadowSOCKS,
            SSHTunnel
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

        public string Cert { get; set; }

        [JsonIgnore] // Prevent JSON serialization
        public string CertKey { get; set; }

        public long SpecteroEngagementId = 0;

        [DataType(DataType.Date)]
        public DateTime CreatedDate{ get; set; }

        [DataType(DataType.Date)]
        public DateTime LastLoginTime { get; set; }

        public override string ToString()
        {
            return "Id -> " + Id + ", AuthKey -> " + AuthKey + ", Password -> " + Password + ", Created At -> " + CreatedDate;
        }
    }
}