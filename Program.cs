using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net.Http;
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
    public bool IsRunning { get; set; }

    public ServiceUpdater()
    {

    }


    public void SetIsRunning(bool running)
    {
        IsRunning = running;
    }

    public bool CheckIfRunning()
    {
        return IsRunning;
    }
}

public interface IServiceUpdater
{
    public bool IsRunning { get; set; }

    public void SetIsRunning(bool running);

    public bool CheckIfRunning();

    public 
}