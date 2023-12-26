# Using Retry Policies on client side for Http Requests to Azure Function Apps

- Outline:
  - Azure Function Apps are running on IIS Servers for both Windows and Linux, where Linux uses a container running on IIS to host the Linux image hosting the Function App
  - This document deploys a Function App where the `/api/http1` endpoint will cause the Function App to fail and start responding with 503 Http Statuses
  - Requests to `/api/http2` we will receive the **Process ID Number** of the current dotnet.exe process running the Function App Custom Code for processing the request


In the [HTMLDoc](https://github.com/macavall/L300-RetryFunctionScenario/tree/master/HtmlDoc) we will find the following JavaScript code for running in a Static Site hosted on the Storage Account


