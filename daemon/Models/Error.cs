using ServiceStack.DataAnnotations;

namespace Spectero.daemon.Models
{
    public class Error : BaseModel
    {
        [EnumAsInt]
        public enum Severity
        {
            Info,
            Warning,
            Error,
            Fatal,
            Debug
        }
    }
}