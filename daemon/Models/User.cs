using System;
using ServiceStack.DataAnnotations;

namespace Spectero.daemon.Models
{
    public class User
    {
        [Index]
        [AutoIncrement]
        public long Id { get; set; }
    
        public string AuthKey { get; set; }
        public string Password { get; set; }
        
        [EnumAsInt]
        public enum Source
        {
            Local,
            Spectero
        }
        public DateTime CreatedDate { get; set; }
        public override string ToString() => "AuthKey -> " + AuthKey + ", Password -> " + Password + ", Created At -> " + CreatedDate;
    }
}