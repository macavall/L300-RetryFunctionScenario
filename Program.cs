using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using TestIso7FA;

public class Program
{
    private static void Main(string[] args)
    {
        var host = new HostBuilder()
        .ConfigureFunctionsWorkerDefaults()
        .ConfigureServices(services =>
        {
            services.AddApplicationInsightsTelemetryWorkerService();
            services.ConfigureFunctionsApplicationInsights();
            services.AddHttpClient();
            services.AddSingleton<IServiceUpdater, ServiceUpdater>();
        })
        .Build();

        host.Run();
    }
}

public class ServiceUpdater : IServiceUpdater
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger _logger;
    private static SemaphoreSlim semaphore = new SemaphoreSlim(30); // Limit to 30 concurrent requests
    private static string endpoint = "https://" + Environment.GetEnvironmentVariable("WEBSITE_HOSTNAME") + "/api/http2"; // "http://localhost:7151/api/http2";
    public static CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(3));
    private static readonly object _lock = new object();

    public bool IsRunning { get; set; }

    public ServiceUpdater(ILoggerFactory loggerFactory, IHttpClientFactory httpClientFactory)
    {
        _logger = loggerFactory.CreateLogger<http2>();
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));

        if (Environment .GetEnvironmentVariable("localrun") == "true")
        {
            endpoint = "http://localhost:7151/api/http2";
        }
    }

    public async Task StartSender()
    {
        _ = Task.Factory.StartNew(async () =>
        {
            await StartHttpSender();
        }, cancellationTokenSource.Token);
    }

    public async Task StartHttpSender()
    {
        //lock (_lock)
        //{
            //_ = Task.Factory.StartNew(async () =>
            //{
                const int numThreads = 100;
                const int durationMinutes = 3;

                //var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(durationMinutes));
                var tasks = new Task[numThreads];

                for (int i = 0; i < numThreads; i++)
                {
                    tasks[i] = SendRequestsAsync(endpoint, cancellationTokenSource.Token, _httpClientFactory);
                }

                await Task.WhenAll(tasks);

                Console.WriteLine("All threads have completed.");
                //created = true;
            //});
        //}
    }

    public async Task CancelHttpSender()
    {
        await Task.Delay(1);
        cancellationTokenSource.Cancel();
        Console.WriteLine("Cancellation token has been cancelled.");

        cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(3));
    }

    public async Task SendRequestsAsync(string endpoint, CancellationToken cancellationToken, IHttpClientFactory clientFactory)
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
                        //if (!ServiceStatus.Running)
                        //{
                        //    break;
                        //}

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

                    if (!ServiceStatus.Running)
                    {
                        break;
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
}

public interface IServiceUpdater
{
    public bool IsRunning { get; set; }

    public Task StartSender();

    public Task StartHttpSender();

    public Task CancelHttpSender();
}