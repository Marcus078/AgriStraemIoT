using System.IO;
using System.Net;
using System.Threading.Tasks;
using Azure.Storage.Queues;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AgriStream.Functions
{
    public class TelemetryIngestFunction
    {
        private readonly ILogger<TelemetryIngestFunction> _logger;
        private readonly IConfiguration _configuration;

        public TelemetryIngestFunction(ILogger<TelemetryIngestFunction> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        [Function("TelemetryIngest")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "telemetry")] HttpRequestData req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a telemetry request.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            _logger.LogInformation($"Received Telemetry Payload: {requestBody}");

            // Get storage connection string from local.settings.json / environment variables
            string connectionString = _configuration["AzureWebJobsStorage"];
            string queueName = "telemetry-queue";

            // Initialize Queue Client and push message
            var queueClient = new QueueClient(connectionString, queueName);
            await queueClient.CreateIfNotExistsAsync();
            await queueClient.SendMessageAsync(requestBody);

            _logger.LogInformation("Telemetry payload successfully pushed to Azure Queue.");

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteStringAsync("Telemetry successfully received and queued.");

            return response;
        }
    }
}