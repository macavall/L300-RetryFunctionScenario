using System.Diagnostics;
using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace TestIso7FA
{
    public class http2
    {
        private readonly ILogger _logger;

        public http2(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<http2>();
        }

        [Function("http2")]
        public HttpResponseData Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

            response.WriteString(Process.GetCurrentProcess().Id.ToString());

            return response;
        }
    }
}
