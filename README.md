# Using Retry Policies on client side for Http Requests to Azure Function Apps

- Outline:
  - Azure Function Apps are running on IIS Servers for both Windows and Linux, where Linux uses a container running on IIS to host the Linux image hosting the Function App
  - This document deploys a Function App where the `/api/http1` endpoint will cause the Function App to fail and start responding with 503 Http Statuses
  - Requests to `/api/http2` we will receive the **Process ID Number** of the current dotnet.exe process running the Function App Custom Code for processing the request


In the [HTMLDoc](https://github.com/macavall/L300-RetryFunctionScenario/tree/master/HtmlDoc) we will find the following JavaScript code for running in a Static Site hosted on the Storage Account

**Enable** the Static Site feature and upload the content of the [HTMLDoc](https://github.com/macavall/L300-RetryFunctionScenario/tree/master/HtmlDoc) and upload using the same file name `RetryJavaScript.html` to the Storage Static Site

![image](https://github.com/macavall/L300-RetryFunctionScenario/assets/43223084/3ff85191-894c-4766-b74f-6557d7fc50d9)

**Copy the Primary Endpoint** and add to the Function App **CORS** configurations

![image](https://github.com/macavall/L300-RetryFunctionScenario/assets/43223084/a6a8f8f0-e58d-484e-8bb7-15278bf8c46a)

Example of the **CORS** Configuration for Function App
- This allows the requests from the Storage Static Site Origin to be accepted by the Function App

![image](https://github.com/macavall/L300-RetryFunctionScenario/assets/43223084/54cae761-9c23-41aa-b448-43a9ebe7a98f)


