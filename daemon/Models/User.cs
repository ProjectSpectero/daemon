using System;
using System.ComponentModel.DataAnnotations;
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

        [ServiceStack.DataAnnotations.Required]
        [ServiceStack.DataAnnotations.StringLength(50)]
        [Index(Unique = true)]
        public string AuthKey { get; set; }

        [ServiceStack.DataAnnotations.Required]
        public string Password { get; set; }

        public string Cert { get; set; }
        public string CertKey { get; set; }

        [DataType(DataType.Date)]
        public DateTime CreatedDate { get; set; }

        public override string ToString()
        {
            return "Id -> " + Id + ", AuthKey -> " + AuthKey + ", Password -> " + Password + ", Created At -> " + CreatedDate;
        }
    }
}