using AgriStreamIoT.Models;
using Microsoft.EntityFrameworkCore;

namespace AgriStreamIoT.Data
{
    public class AgriDbContext : DbContext
    {
        public AgriDbContext(DbContextOptions<AgriDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Device> Devices { get; set; }
        public DbSet<TelemetryReading> TelemetryReadings { get; set; }
    }
}