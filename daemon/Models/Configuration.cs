using System;
using ServiceStack.DataAnnotations;

namespace Spectero.daemon.Models
{
    public class Configuration : IModel
    {
        [Index]
        [AutoIncrement]
        public int Id { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }

        [EnumAsInt]
        public enum Type
        {
            Generic,
            Service
        }
    }
}
