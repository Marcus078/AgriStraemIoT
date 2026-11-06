using AgriStreamIoT.Data;
using AgriStreamIoT.Models;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AgriStreamIoT.Controllers
{
    [Authorize]
    public class DevicesController : Controller
    {
        private readonly AgriDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;

        public DevicesController(AgriDbContext context, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
        }

        // GET: Devices (Accessible to any logged-in user)
        public async Task<IActionResult> Index()
        {
            var devices = await _context.Devices.ToListAsync();
            return View(devices);
        }

        // ==========================================
        // CREATE ACTIONS (Admin Only)
        // ==========================================

        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([Bind("DeviceId,DeviceName,Location,OperationalStatus,LastUpdated")] Device device)
        {
            if (ModelState.IsValid)
            {
                device.LastUpdated = DateTime.UtcNow;
                _context.Add(device);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(device);
        }

        // ==========================================
        // EDIT ACTIONS (Admin Only)
        // ==========================================

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var device = await _context.Devices.FindAsync(id);
            if (device == null)
            {
                return NotFound();
            }
            return View(device);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(string id, [Bind("DeviceId,DeviceName,Location,OperationalStatus,LastUpdated")] Device device)
        {
            if (id != device.DeviceId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    device.LastUpdated = DateTime.UtcNow;
                    _context.Update(device);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DeviceExists(device.DeviceId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(device);
        }

        // ==========================================
        // DELETE ACTIONS (Admin Only)
        // ==========================================

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var device = await _context.Devices.FirstOrDefaultAsync(m => m.DeviceId == id);
            if (device == null)
            {
                return NotFound();
            }

            return View(device);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var device = await _context.Devices.FindAsync(id);
            if (device != null)
            {
                _context.Devices.Remove(device);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool DeviceExists(string id)
        {
            return _context.Devices.Any(e => e.DeviceId == id);
        }

        // ==========================================
        // AZURE FUNCTIONS INTEGRATION ACTIONS
        // ==========================================

        // GET: Devices/GetTelemetryData (Calls HTTP GET Azure Function)
        [HttpGet]
        public async Task<IActionResult> GetTelemetryData()
        {
            try
            {
                var client = _httpClientFactory.CreateClient("AzureFunctions");
                var response = await client.GetAsync("telemetry");

                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    return Content(jsonString, "application/json");
                }
            }
            catch (Exception)
            {
                // Fallback or empty list if function app is offline locally
            }

            return Json(new List<object>());
        }

        // POST: Devices/TriggerIrrigation (Calls HTTP POST Azure Function)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TriggerIrrigation(string deviceId)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("AzureFunctions");
                var payload = new
                {
                    DeviceId = deviceId,
                    Command = "TriggerIrrigation",
                    Timestamp = DateTime.UtcNow
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(payload),
                    Encoding.UTF8,
                    "application/json"
                );

                var response = await client.PostAsync("irrigation", content);

                if (response.IsSuccessStatusCode)
                {
                    TempData["Message"] = $"Irrigation command successfully queued for device {deviceId}!";
                }
                else
                {
                    TempData["Error"] = "Failed to dispatch irrigation command to Azure Function.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error connecting to Azure Functions: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }
        // GET: Devices/UploadDroneImage
        [HttpGet]
        public IActionResult UploadDroneImage()
        {
            return View();
        }

        // POST: Devices/UploadDroneImage
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadDroneImage(IFormFile droneImage)
        {
            if (droneImage == null || droneImage.Length == 0)
            {
                ModelState.AddModelError("", "Please select a valid image file.");
                return View();
            }

            try
            {
                // Get storage connection string from configuration or environment
                string connectionString = HttpContext.RequestServices.GetRequiredService<IConfiguration>()
                    .GetConnectionString("AzureWebJobsStorage") ?? Environment.GetEnvironmentVariable("AzureWebJobsStorage");

                string containerName = "drone-images";

                var blobServiceClient = new BlobServiceClient(connectionString);
                var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
                await containerClient.CreateIfNotExistsAsync();

                // Generate unique file name to prevent naming collisions
                string fileName = $"{Guid.NewGuid()}_{Path.GetFileName(droneImage.FileName)}";
                var blobClient = containerClient.GetBlobClient(fileName);

                using (var stream = droneImage.OpenReadStream())
                {
                    await blobClient.UploadAsync(stream, true);
                }

                TempData["Message"] = $"Drone image '{droneImage.FileName}' successfully uploaded! The Blob Trigger will process it shortly.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error uploading image: {ex.Message}");
                return View();
            }
        }
    }
}