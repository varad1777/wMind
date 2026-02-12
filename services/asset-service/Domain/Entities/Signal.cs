namespace Domain.Entities
{
    public class Signal
    {
        public Guid SignalId { get; set; }

        public string SignalKey { get; set; }

        public Guid AssetId { get; set; }
        public Guid DeviceId { get; set; }

        public string SignalName { get; set; }
        public string Unit { get; set; }

        public DateTime CreatedAt { get; set; }

        public Guid SignalTypeId { get; set; }

        public SignalTypes SignalType { get; set; }


    }
}
