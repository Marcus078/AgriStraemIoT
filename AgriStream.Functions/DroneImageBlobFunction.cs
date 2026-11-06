using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace AgriStream.Functions
{
    public class DroneImageBlobFunction
    {
        private readonly ILogger<DroneImageBlobFunction> _logger;

        public DroneImageBlobFunction(ILogger<DroneImageBlobFunction> logger)
        {
            _logger = logger;
        }

        [Function("DroneImageBlobTrigger")]
        public async Task Run(
            [BlobTrigger("drone-images/{name}", Connection = "AzureWebJobsStorage")] Stream stream,
            string name)
        {
            _logger.LogInformation($"C# Blob trigger function processed blob\n Name:{name} \n Size: {stream.Length} bytes");

            // Here is where you would log the audit entry into Azure Table Storage
            await Task.CompletedTask;
        }
    }
}