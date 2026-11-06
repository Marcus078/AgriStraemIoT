using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace AgriStream.Functions
{
    public class GetTelemetryFunction
    {
        private readonly ILogger<GetTelemetryFunction> _logger;

        public GetTelemetryFunction(ILogger<GetTelemetryFunction> logger)
        {
            _logger = logger;
        }

        [Function("GetTelemetry")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "telemetry")] HttpRequestData req)
        {
            _logger.LogInformation("HTTP GET trigger function processed a request for telemetry.");

            var response = req.CreateResponse(HttpStatusCode.OK);

            // Returns sample/telemetry data for the MVC dashboard chart
            var telemetryData = new[]
            {
                new { timestamp = DateTime.UtcNow.AddMinutes(-10).ToString("HH:mm:ss"), temperature = 23.5, soilMoisture = 60.2 },
                new { timestamp = DateTime.UtcNow.AddMinutes(-5).ToString("HH:mm:ss"), temperature = 24.1, soilMoisture = 58.9 },
                new { timestamp = DateTime.UtcNow.ToString("HH:mm:ss"), temperature = 24.8, soilMoisture = 61.4 }
            };

            await response.WriteAsJsonAsync(telemetryData);
            return response;
        }
    }
}