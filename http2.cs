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
        private readonly IServiceUpdater _serviceUpdater;

        public http2(ILoggerFactory loggerFactory, IServiceUpdater serviceUpdater)
        {
            _logger = loggerFactory.CreateLogger<http2>();
            _serviceUpdater = serviceUpdater;
        }

        [Function("http2")]
        public HttpResponseData Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            string result = req.Query["start"];

            if(result == "true")
            {
                _serviceUpdater.StartSender();
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

            response.WriteString(Process.GetCurrentProcess().Id.ToString());

            return response;
        }
    }
}
