using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace TestIso7FA
{
    public class http2
    {
        private readonly ILogger _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private static string endpoint = "https://" + Environment.GetEnvironmentVariable("WEBSITE_HOSTNAME") + "/api/http2";
        private static bool created = true;
        private static SemaphoreSlim semaphore = new SemaphoreSlim(30); // Limit to 5 concurrent requests

        public http2(ILoggerFactory loggerFactory, IHttpClientFactory httpClientFactory)
        {
            _logger = loggerFactory.CreateLogger<http2>();
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        }

        [Function("http2")]
        public HttpResponseData Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");
            if (Environment.GetEnvironmentVariable("localrun") == "true")
            {
                endpoint = "http://localhost:7151/api/http2";
            }
            _logger.LogInformation(endpoint);

            if (created)
            {
                created = false;
                _ = Task.Factory.StartNew(async () =>
                {
                    const int numThreads = 100;
                    const int durationMinutes = 3;

                    var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(durationMinutes));
                    var tasks = new Task[numThreads];

                    for (int i = 0; i < numThreads; i++)
                    {
                        tasks[i] = SendRequestsAsync(endpoint, cancellationTokenSource.Token, _httpClientFactory);
                    }

                    await Task.WhenAll(tasks);

                    Console.WriteLine("All threads have completed.");
                    //created = true;
                });
            }



            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

            response.WriteString(Process.GetCurrentProcess().Id.ToString());

            return response;
        }

        static async Task SendRequestsAsync(string endpoint, CancellationToken cancellationToken, IHttpClientFactory clientFactory)
        {
            await semaphore.WaitAsync(); // Wait for an open slot

            try
            {
                using (var httpClient = clientFactory.CreateClient())
                {
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        int retryCount = 0;
                        bool requestSuccessful = false;

                        while (!requestSuccessful && retryCount < 100)
                        {
                            try
                            {
                                HttpResponseMessage response = await httpClient.GetAsync(endpoint, cancellationToken);

                                if (response.IsSuccessStatusCode)
                                {
                                    Console.WriteLine($"Request successful. Thread: {Thread.CurrentThread.ManagedThreadId}");
                                    requestSuccessful = true;
                                }
                                else if ((int)response.StatusCode >= 500)
                                {
                                    Console.WriteLine($"Request failed with status code {response.StatusCode}. Thread: {Thread.CurrentThread.ManagedThreadId}, Retry Count: {retryCount}");
                                    retryCount++;
                                    await Task.Delay(3000); // Delay between retries (1 second)
                                }
                                else
                                {
                                    Console.WriteLine($"Request failed. Thread: {Thread.CurrentThread.ManagedThreadId}, Status Code: {response.StatusCode}");
                                    requestSuccessful = true; // Exit the loop if the status code is less than 500
                                }
                            }
                            catch (HttpRequestException ex)
                            {
                                Console.WriteLine($"Request And Retry Failed. Thread: {Thread.CurrentThread.ManagedThreadId}, Exception: {ex.Message}");
                                requestSuccessful = true; // Exit the loop if an exception occurs
                            }
                        }

                        await Task.Delay(500); // Delay between requests (1 second)
                    }
                }
            }
            finally
            {
                semaphore.Release(); // Release the slot
            }
        }

        //static async Task SendRequestsAsync(string endpoint, CancellationToken cancellationToken, IHttpClientFactory clientFactory)
        //{
        //    await semaphore.WaitAsync(); // Wait for an open slot

        //    try
        //    {
        //        using (var httpClient = clientFactory.CreateClient())
        //        {
        //            while (!cancellationToken.IsCancellationRequested)
        //            {
        //                try
        //                {
        //                    HttpResponseMessage response = await httpClient.GetAsync(endpoint, cancellationToken);

        //                    if (response.IsSuccessStatusCode)
        //                    {
        //                        Console.WriteLine($"Request successful. Thread: {Thread.CurrentThread.ManagedThreadId}");
        //                    }
        //                    else
        //                    {
        //                        Console.WriteLine($"Request failed. Thread: {Thread.CurrentThread.ManagedThreadId}, Status Code: {response.StatusCode}");
        //                    }
        //                }
        //                catch (HttpRequestException ex)
        //                {
        //                    Console.WriteLine($"Request failed. Thread: {Thread.CurrentThread.ManagedThreadId}, Exception: {ex.Message}");
        //                }

        //                await Task.Delay(500); // Delay between requests (1 second)
        //            }
        //        }
        //    }
        //    finally
        //    {
        //        semaphore.Release(); // Release the slot
        //    }

        //    //await semaphore.WaitAsync(); // Wait for an open slot

        //    //using (var httpClient = clientFactory.CreateClient()) //var httpClient = new HttpClient())
        //    //{
        //    //    while (!cancellationToken.IsCancellationRequested)
        //    //    {
        //    //        try
        //    //        {
        //    //            HttpResponseMessage response = await httpClient.GetAsync(endpoint, cancellationToken);

        //    //            if (response.IsSuccessStatusCode)
        //    //            {
        //    //                Console.WriteLine($"Request successful. Thread: {Thread.CurrentThread.ManagedThreadId}");
        //    //            }
        //    //            else
        //    //            {
        //    //                Console.WriteLine($"Request failed. Thread: {Thread.CurrentThread.ManagedThreadId}, Status Code: {response.StatusCode}");
        //    //            }
        //    //        }
        //    //        catch (HttpRequestException ex)
        //    //        {
        //    //            Console.WriteLine($"Request failed. Thread: {Thread.CurrentThread.ManagedThreadId}, Exception: {ex.Message}");
        //    //        }

        //    //        await Task.Delay(10); // Delay between requests (1 second)
        //    //    }
        //    //}
        //}

    }
}
