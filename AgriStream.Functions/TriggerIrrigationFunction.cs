using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace AgriStream.Functions
{
    public class TriggerIrrigationFunction
    {
        private readonly ILogger<TriggerIrrigationFunction> _logger;

        public TriggerIrrigationFunction(ILogger<TriggerIrrigationFunction> logger)
        {
            _logger = logger;
        }

        [Function("TriggerIrrigation")]
        [QueueOutput("irrigation-queue", Connection = "AzureWebJobsStorage")]
        public async Task<string> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "irrigation")] HttpRequestData req)
        {
            _logger.LogInformation("HTTP POST trigger received irrigation command.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            _logger.LogInformation($"Queuing irrigation command payload: {requestBody}");

            // Returning this string automatically sends it to the "irrigation-queue" storage queue
            return requestBody;
        }
    }
}