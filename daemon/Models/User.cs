using System;
using ServiceStack.DataAnnotations;

namespace Spectero.daemon.Models
{
    public class User
    {
        public long Id { get; set; }
    
        [Index]
        public string Name { get; set; }
        public DateTime CreatedDate { get; set; }
        public override string ToString() => Name;
    }
}