using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using TestIso7FA;

public class Program
{
    private static void Main(string[] args)
    {
        var myDependency = new MyBeforeStartupDependency();

        var host = new HostBuilder()
        .ConfigureFunctionsWorkerDefaults()
        .ConfigureServices(services =>
        {
            services.AddApplicationInsightsTelemetryWorkerService();
            services.ConfigureFunctionsApplicationInsights();
            services.AddHttpClient();
            services.AddSingleton<IServiceUpdater, ServiceUpdater>();
            services.AddSingleton(new MyBeforeStartupDependency());
            //services.AddSingleton<MyBeforeStartupDependency>(myDependency);
            services.AddSingleton<MyAfterStartupDependency>();
        })
        .Build();

        host.Run();

        Console.WriteLine("Function App Host has started up!");
    }
}

public static class MemoryClass
{
    public static List<byte[]> memoryLeakList = new List<byte[]>();

    public static void AddMemory(int mb)
    {
        for (int i = 0; i < mb; i++)
        {
            // Simulate adding 1 MB of data to the list
            byte[] data = new byte[1024 * 1024]; // 1 MB
            memoryLeakList.Add(data);

            Console.WriteLine($"Total memory allocated: {GC.GetTotalMemory(false) / (1024 * 1024)} MB");

            // Sleep for a short time to slow down memory consumption for demonstration
            System.Threading.Thread.Sleep(100);
        }
    }
}

public class MyBeforeStartupDependency// : IDisposable
{
    private readonly ILogger logger;
    public static Guid guid = Guid.NewGuid();

    public MyBeforeStartupDependency()
    {
        logger = LoggerFactory.Create(config =>
        {
            config.AddConsole();
        }).CreateLogger("MyBeforeStartupDependency");

        logger.LogInformation("MyBeforeStartupDependency created");
    }

    public void ShowGuid(int mb = 100)
    {
        Console.WriteLine($"MyBeforeStartupDependency guid: {guid}");

        MemoryClass.AddMemory(mb);
    }

    //public void Dispose() => Console.WriteLine("MyBeforeStartupDependency disposed");
}

public class MyAfterStartupDependency
{
    private readonly ILogger logger;

    public MyAfterStartupDependency()
    {
        logger = LoggerFactory.Create(config =>
        {
            config.AddConsole();
        }).CreateLogger("MyAfterStartupDependency");

        logger.LogInformation("MyAfterStartupDependency created");
    }
}

public class ServiceUpdater : IServiceUpdater
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger _logger;
    private static SemaphoreSlim semaphore = new SemaphoreSlim(30); // Limit to 30 concurrent requests
    private static string endpoint = "https://" + Environment.GetEnvironmentVariable("WEBSITE_HOSTNAME") + "/api/http2"; // "http://localhost:7151/api/http2";
    public static CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
    private static readonly object _lock = new object();

    public bool IsRunning { get; set; }

    public ServiceUpdater(ILoggerFactory loggerFactory, IHttpClientFactory httpClientFactory)
    {
        _logger = loggerFactory.CreateLogger<http2>();
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));

        if (Environment.GetEnvironmentVariable("localrun") == "true")
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

        cancellationTokenSource = new CancellationTokenSource();
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