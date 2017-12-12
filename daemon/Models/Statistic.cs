using System;
using ServiceStack.DataAnnotations;
using Spectero.daemon.Libraries.Core.Statistics;

namespace Spectero.daemon.Models
{
    public class Statistic : BaseModel
    {
        [EnumAsInt]
        public enum SampleSize
        {
            Minute,
            Hour,
            Day,
            Month,
            Year
        }

        [EnumAsInt]
        public enum Service
        {
            HTTPProxy,
            VPN,
            SSH
        }


        public long Bytes { get; set; }

        //Todo: check if this actually gets serialized properly
        public DataFlowDirections Directions { get; set; }
    }
}