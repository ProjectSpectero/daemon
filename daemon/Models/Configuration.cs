using ServiceStack.DataAnnotations;

namespace Spectero.daemon.Models
{
    public class Configuration : BaseModel
    {
        [EnumAsInt]
        public enum Type
        {
            Generic,
            Service
        }

        public string Key { get; set; }
        public string Value { get; set; }
    }
}