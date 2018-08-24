using System;

namespace Spectero.daemon.Models
{
    public class Seeder : BaseModel
    {
        public string Version { get; set; }
        public DateTime AppliedOn { get; set; }
        public string Description { get; set; }
    }
}