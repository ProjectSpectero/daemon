using System;
using ServiceStack.DataAnnotations;

namespace Spectero.daemon.Models
{
    public class User : IModel
    {
        [EnumAsInt]
        public enum Source
        {
            Local,
            Spectero
        }

        [Index]
        [AutoIncrement]
        public long Id { get; set; }

        public string AuthKey { get; set; }
        public string Password { get; set; }

        public string Cert { get; set; }
        public string CertKey { get; set; }
        public DateTime CreatedDate { get; set; }

        public override string ToString()
        {
            return "AuthKey -> " + AuthKey + ", Password -> " + Password + ", Created At -> " + CreatedDate;
        }
    }
}