using System;
using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace TestIso7FA
{
    public class http3
    {
        private readonly ILogger _logger;
        private readonly MyBeforeStartupDependency myBeforeStartupDepdendency;

        public http3(ILoggerFactory loggerFactory, MyBeforeStartupDependency _myBeforeStartupDependency)
        {
            _logger = loggerFactory.CreateLogger<http3>();
            myBeforeStartupDepdendency = _myBeforeStartupDependency;
        }

        [Function("http3")]
        public HttpResponseData Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            var mb = req.Query["mb"];

            myBeforeStartupDepdendency.ShowGuid(Convert.ToInt32(mb));
            //myBeforeStartupDepdendency.Dispose();

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

            response.WriteString("Welcome to Azure Functions!");

            return response;
        }
    }
}
