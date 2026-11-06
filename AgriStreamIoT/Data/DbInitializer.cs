using AgriStreamIoT.Models;
using Microsoft.EntityFrameworkCore;

namespace AgriStreamIoT.Data
{
    public static class DbInitializer
    {
        public static void Initialize(AgriDbContext context)
        {
            // Check if users already exist
            if (context.Users.Any())
            {
                return; // Database has already been seeded
            }

            // Seed default users
            var users = new User[]
            {
                new User { Username = "admin", Password = "Password123!", Role = "Admin" },
                new User { Username = "farmer_john", Password = "Password123!", Role = "Farmer" },
                new User { Username = "agronomist_sarah", Password = "Password123!", Role = "Agronomist" }
            };

            context.Users.AddRange(users);

            // Seed initial IoT devices
            var devices = new Device[]
            {
                new Device { DeviceId = "DEV-001", DeviceName = "North Field Sensor", Location = "Sector A - Maize", OperationalStatus = "Active" },
                new Device { DeviceId = "DEV-002", DeviceName = "Greenhouse Hydration Node", Location = "Greenhouse 2", OperationalStatus = "Irrigating" },
                new Device { DeviceId = "DEV-003", DeviceName = "South Orchard Monitor", Location = "Orchard B - Citrus", OperationalStatus = "Maintenance" }
            };

            context.Devices.AddRange(devices);
            context.SaveChanges();
        }
    }
}