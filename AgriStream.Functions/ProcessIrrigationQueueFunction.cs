using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace AgriStream.Functions
{
    public class ProcessIrrigationQueueFunction
    {
        private readonly ILogger<ProcessIrrigationQueueFunction> _logger;

        public ProcessIrrigationQueueFunction(ILogger<ProcessIrrigationQueueFunction> logger)
        {
            _logger = logger;
        }

        [Function("ProcessIrrigationQueue")]
        public async Task Run(
            [QueueTrigger("irrigation-queue", Connection = "AzureWebJobsStorage")] string message)
        {
            _logger.LogInformation($"[Queue Trigger] Processing irrigation command from queue: {message}");

            // TODO: Add your business logic here 
            // (e.g., updating database device status to "Irrigating", calling sprinkler IoT API, etc.)

            await Task.CompletedTask;
            _logger.LogInformation("[Queue Trigger] Irrigation command processed successfully.");
        }
    }
}