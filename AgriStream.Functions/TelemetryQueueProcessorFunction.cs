using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Queues;
using AgriStreamIoT.Data;
using AgriStreamIoT.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AgriStream.Functions
{
    public class TelemetryQueueProcessorService : BackgroundService
    {
        private readonly ILogger<TelemetryQueueProcessorService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IServiceScopeFactory _scopeFactory;

        public TelemetryQueueProcessorService(
            ILogger<TelemetryQueueProcessorService> logger,
            IConfiguration configuration,
            IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _configuration = configuration;
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Telemetry Queue Processor background service started.");

            string connectionString = _configuration["AzureWebJobsStorage"];
            string queueName = "telemetry-queue";
            var queueClient = new QueueClient(connectionString, queueName);

            // Ensure queue exists on startup
            try
            {
                await queueClient.CreateIfNotExistsAsync(cancellationToken: stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Could not verify queue on startup: {ex.Message}");
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (await queueClient.ExistsAsync(stoppingToken))
                    {
                        var messages = await queueClient.ReceiveMessagesAsync(maxMessages: 10, cancellationToken: stoppingToken);

                        foreach (var message in messages.Value)
                        {
                            _logger.LogInformation($"[Queue Processor] Processing telemetry payload: {message.Body}");

                            // Create a DI scope to resolve scoped services like AgriDbContext safely
                            using (var scope = _scopeFactory.CreateScope())
                            {
                                var dbContext = scope.ServiceProvider.GetRequiredService<AgriDbContext>();

                                // Optional: Deserialize and save to Azure SQL if your model matches your JSON structure
                                // var telemetry = JsonSerializer.Deserialize<TelemetryReading>(message.Body.ToString());
                                // if (telemetry != null)
                                // {
                                //     dbContext.TelemetryReadings.Add(telemetry);
                                //     await dbContext.SaveChangesAsync(stoppingToken);
                                //     _logger.LogInformation("Telemetry successfully saved to Azure SQL database!");
                                // }
                            }

                            // Delete message after successful processing
                            await queueClient.DeleteMessageAsync(message.MessageId, message.PopReceipt, stoppingToken);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error processing queue messages: {ex.Message}");
                }

                // Poll every 10 seconds
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
    }
}