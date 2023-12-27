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
    public class http1
    {
        private readonly ILogger _logger;
        private static readonly ManualResetEventSlim eventSlim = new ManualResetEventSlim(false);
        private static int threadCount = 600;
        private readonly IServiceUpdater _serviceUpdater;

        public http1(ILoggerFactory loggerFactory, IServiceUpdater serviceUpdater)
        {
            _logger = loggerFactory.CreateLogger<http1>();
            _serviceUpdater = serviceUpdater;
        }

        [Function("http1")]
        public HttpResponseData Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            _serviceUpdater.CancelHttpSender();

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

            ServiceStatus.Running = false;


            if (Environment.GetEnvironmentVariable("localrun") != "true")
            {
                StartHighThreadCount();
            }
            

            response.WriteString(Process.GetCurrentProcess().Id.ToString());

            return response;
        }

        public void StartHighThreadCount()
        {
            for (int i = 0; i < threadCount; i++)
            {
                Thread thread = new Thread(DoWork);
                thread.Start(i);
            }

            // Signal the event to start all the threads.
            eventSlim.Set();

            // Keep the main thread alive until all other threads are done.
            while (threadCount > 0)
            {
                Thread.Sleep(100);
            }

            eventSlim.Dispose();
        }

        public void DoWork(object data)
        {
            int threadNumber = (int)data;

            // Wait for the event to be signaled.
            eventSlim.Wait();

            Console.WriteLine($"Thread {threadNumber} is doing some work.");

            // Simulate some work.
            Thread.Sleep(600000);

            Interlocked.Decrement(ref threadCount);
        }
    }
}
