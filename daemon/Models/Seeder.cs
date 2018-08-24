using System;

namespace Spectero.daemon.Models
{
    public class Seeder : BaseModel
    {
        public string Version { get; set; }
        public DateTime AppliedOn = DateTime.UtcNow;
        public string Description { get; set; }
    }
}