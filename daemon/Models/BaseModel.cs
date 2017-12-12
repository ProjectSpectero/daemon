using System;
using System.ComponentModel.DataAnnotations;
using ServiceStack.DataAnnotations;

namespace Spectero.daemon.Models
{
    public class BaseModel : IModel
    {
        [Index]
        [AutoIncrement]
        public long Id { get; set; }

        [DataType(DataType.Date)]
        public DateTime CreatedDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime UpdatedDate { get; set; }
    }
}