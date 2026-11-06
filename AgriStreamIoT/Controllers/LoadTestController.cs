using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace AgriStream.Controllers
{
    public class LoadTestController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public LoadTestController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        // GET: /LoadTest/Run
        public async Task<IActionResult> Run(int totalRequests = 50, int concurrency = 10)
        {
            var client = _httpClientFactory.CreateClient();
            // Automatically points to your running app's devices page
            string targetUrl = $"{Request.Scheme}://{Request.Host}/Devices";

            var stopwatch = Stopwatch.StartNew();
            var results = new ConcurrentBag<(bool IsSuccess, long ElapsedMilliseconds)>();

            var semaphore = new SemaphoreSlim(concurrency);
            var tasks = new List<Task>();

            for (int i = 0; i < totalRequests; i++)
            {
                await semaphore.WaitAsync();

                tasks.Add(Task.Run(async () =>
                {
                    var reqWatch = Stopwatch.StartNew();
                    try
                    {
                        var response = await client.GetAsync(targetUrl);
                        reqWatch.Stop();
                        results.Add((response.IsSuccessStatusCode, reqWatch.ElapsedMilliseconds));
                    }
                    catch
                    {
                        reqWatch.Stop();
                        results.Add((false, reqWatch.ElapsedMilliseconds));
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }));
            }

            await Task.WhenAll(tasks);
            stopwatch.Stop();

            // Calculate stats
            int successCount = 0;
            int failCount = 0;
            long totalTimeMs = 0;

            foreach (var r in results)
            {
                if (r.IsSuccess) successCount++;
                else failCount++;
                totalTimeMs += r.ElapsedMilliseconds;
            }

            double avgLatency = results.Count > 0 ? (double)totalTimeMs / results.Count : 0;
            double throughput = stopwatch.ElapsedMilliseconds > 0 ? totalRequests / (stopwatch.ElapsedMilliseconds / 1000.0) : 0;

            // Return clean formatted text directly to the browser (No frontend view needed!)
            string htmlResult = $@"
                <html>
                <body style='font-family: Arial; padding: 40px; background: #f4f6f8;'>
                    <div style='max-width: 600px; margin: auto; background: white; padding: 30px; border-radius: 8px; box-shadow: 0 4px 10px rgba(0,0,0,0.1);'>
                        <h2 style='color: #2e7d32; margin-top: 0;'>AgriStream Native C# Load Test Results</h2>
                        <hr style='border: 0; border-top: 1px solid #ddd;'/>
                        <p><b>Target URL:</b> {targetUrl}</p>
                        <p><b>Total Requests Simulated:</b> {totalRequests}</p>
                        <p><b>Concurrency Level:</b> {concurrency} active threads</p>
                        <p><b>Total Duration:</b> {stopwatch.ElapsedMilliseconds / 1000.0:F2} seconds</p>
                        <p><b>Successful Requests (HTTP 200):</b> <span style='color: green; font-weight: bold;'>{successCount}</span></p>
                        <p><b>Failed Requests:</b> <span style='color: red; font-weight: bold;'>{failCount}</span></p>
                        <p><b>Average Latency:</b> {avgLatency:F2} ms</p>
                        <p><b>Throughput:</b> {throughput:F2} requests/sec</p>
                        <br/>
                        <a href='/Devices' style='background: #2e7d32; color: white; padding: 10px 20px; text-decoration: none; border-radius: 4px;'>Back to Dashboard</a>
                    </div>
                </body>
                </html>";

            return Content(htmlResult, "text/html");
        }
    }
}