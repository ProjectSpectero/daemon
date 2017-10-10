using System;
using ServiceStack.DataAnnotations;

namespace Spectero.daemon.Models
{
    public class Statistic : IModel
    {
        [Index]
        [AutoIncrement]
        public long Id { get; set; }
        
        public long BytesIn { get; set; }
        public long BytesOut { get; set; }
        public DateTime CreationTime { get; set; }

        [EnumAsInt]
        public enum SampleSize
        {
            Minute,
            Hour,
            Day,
            Month,
            Year
        }
    }
}