using System.ComponentModel.DataAnnotations;

namespace AgriStreamIoT.Models
{
    public class Device
    {

        [Key]
        [Required]
        public string DeviceId { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string DeviceName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Location { get; set; } = string.Empty;

        public string OperationalStatus { get; set; } = "Active"; // Active, Irrigating, Maintenance

        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}
