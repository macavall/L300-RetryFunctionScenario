# Using Retry Policies on client side for Http Requests to Azure Function Apps

- Outline:
  - Azure Function Apps are running on IIS Servers for both Windows and Linux, where Linux uses a container running on IIS to host the Linux image hosting the Function App
  - This document deploys a Function App where the `/api/http1` endpoint will cause the Function App to fail and start responding with 503 Http Statuses
  - Requests to `/api/http2` we will receive the **Process ID Number** of the current dotnet.exe process running the Function App Custom Code for processing the request


---

# Storage Static Site Configuration

- In the [HTMLDoc](https://github.com/macavall/L300-RetryFunctionScenario/tree/master/HtmlDoc) we will find the following JavaScript code for running in a Static Site hosted on the Storage Account

- **Enable** the Static Site feature and upload the content of the [HTMLDoc](https://github.com/macavall/L300-RetryFunctionScenario/tree/master/HtmlDoc) and upload using the same file name `RetryJavaScript.html` to the Storage Static Site

![image](https://github.com/macavall/L300-RetryFunctionScenario/assets/43223084/3ff85191-894c-4766-b74f-6557d7fc50d9)

- **Copy the Primary Endpoint** and add to the Function App **CORS** configurations

![image](https://github.com/macavall/L300-RetryFunctionScenario/assets/43223084/a6a8f8f0-e58d-484e-8bb7-15278bf8c46a)

- Example of the **CORS** Configuration for Function App
  - This allows the requests from the Storage Static Site Origin to be accepted by the Function App

![image](https://github.com/macavall/L300-RetryFunctionScenario/assets/43223084/571f85a6-478d-4d23-bfe0-6fbccbc40e4d)

## Resulting site will look like this

![image](https://github.com/macavall/L300-RetryFunctionScenario/assets/43223084/de79291e-7eb0-429e-9697-6da4fd36ed32)

- Open the F12 Developer Tools in the Browser loading the Storage Static Site
  - This is how to open the F12 Developer Tools in Microsoft Edge (_You can also press F12 on the Keyboard to open the Developer Tools_)

![image](https://github.com/macavall/L300-RetryFunctionScenario/assets/43223084/21ae9b62-79f5-4c4e-a564-d0a55af4d314)

- Open the **Networking** blade to see the Network Requests in action

![image](https://github.com/macavall/L300-RetryFunctionScenario/assets/43223084/35318b0e-335e-42a7-88de-05f7b56ea671)

---

## Reproduction Steps

1. Once the Function App is deployed, open the HTML Static Site and attempt running the request and confirm the Function App is responding as desired

Deploy the Scenario here: [![Deploy To Azure](https://raw.githubusercontent.com/Azure/azure-quickstart-templates/master/1-CONTRIBUTION-GUIDE/images/deploytoazure.svg?sanitize=true)](https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2Fmacavall%2FL300-RetryFunctionScenario%2Fmaster%2Fazuredeploy.json)

3. Now run the request to `/api/http2` as this will have the Function App start to respond with 503 Http Statuses (`Service Unavailable`)
