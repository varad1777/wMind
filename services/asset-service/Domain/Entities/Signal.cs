namespace Domain.Entities
{
    public class Signal
    {
        public Guid SignalId { get; set; }   // internal unique id (GUID)

        public string SignalKey { get; set; } // PLANT1.ASSET1.DEV1.TEMP

        public Guid AssetId { get; set; }
        public Guid DeviceId { get; set; }

        public string SignalName { get; set; } // TEMP, PRESSURE, SPEED
        public string Unit { get; set; }       // °C, bar, rpm

        public DateTime CreatedAt { get; set; }
    }
}