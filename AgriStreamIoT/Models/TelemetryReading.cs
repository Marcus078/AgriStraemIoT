using System;
using System.ComponentModel.DataAnnotations;

namespace AgriStreamIoT.Models
{
    public class TelemetryReading
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string DeviceId { get; set; } = string.Empty;

        public double Temperature { get; set; }

        public double SoilMoisture { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}