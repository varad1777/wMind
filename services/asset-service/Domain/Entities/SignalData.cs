using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class SignalData
    {
        [Key]
        public long SignalDataId { get; set; }

        [Required] public Guid AssetId { get; set; }
        [Required] public Guid SignalTypeId { get; set; }
        [Required] public Guid DeviceId { get; set; }
        [Required] public Guid DevicePortId { get; set; }

        [MaxLength(200)] public string SignalName { get; set; }
        [MaxLength(50)] public string SignalUnit { get; set; }
        public int? RegisterAddress { get; set; }

        [Required] public DateTime BucketStartUtc { get; set; }

        [Required] public int Count { get; set; }
        [Required] public double Sum { get; set; }
        public double? MinValue { get; set; }
        public double? MaxValue { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public double? AvgValue { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
