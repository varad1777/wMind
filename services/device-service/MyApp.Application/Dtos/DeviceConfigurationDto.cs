using System.ComponentModel.DataAnnotations;

namespace MyApp.Application.Dtos
{
    public class DeviceConfigurationDto
    {
        [Required(ErrorMessage = "Configuration name is required.")]
        [StringLength(100, MinimumLength = 1, ErrorMessage = "Configuration name must be between 1 and 100 characters.")]
        public string Name { get; set; } = null!;

        [Range(100, 300000, ErrorMessage = "Poll interval must be between 100 and 300000 milliseconds.")]
        public int PollIntervalMs { get; set; } = 1000;

        public string IpAddress { get; set; } = "127.0.0.1";
        public int Port { get; set; } = 502;
        public byte SlaveId { get; set; } = 1;
        public string Endian { get; set; } = "Little";

    }


}
