# azure-functions-ai-privateavailtests-dotnet6
This project is a sample .NET 6 function app which defines Timer/Http trigger function(s) that initiate an async http request to remotely call an app service that is isolated from public network access for Availability Test call(s) that is then logged into App Insights.

# <h1>Repro Steps</h1>

The project was setup following the below documentation.

<ul>
  <li>
    <a href="https://learn.microsoft.com/en-us/azure/azure-monitor/app/availability-private-test#disconnected-or-no-ingress-scenarios" target="_blank">Disconnected or no ingress scenarios (Azure Monitor)</a>
  </li>
  <li>
    <a href="https://learn.microsoft.com/en-us/azure/azure-monitor/app/availability-azure-functions" target="_blank">Create and run custom availability tests by using Azure Functions</a>
  </li>
</ul>

NOTE: Project setup using VS 2022 installed with Azure specific workloads - see documentation below:
<a href="https://docs.microsoft.com/en-us/dotnet/azure/configure-visual-studio" target="_blank">Configure Visual Studio for Azure development with .NET</a>
<a href="https://docs.microsoft.com/en-us/visualstudio/azure/overview-azure-integration?view=vs-2022" target="_blank">Overview: Azure integration</a>

Dependencies:
<ol>
  <li>
    <a href="https://www.nuget.org/packages/Microsoft.ApplicationInsights/" target="_blank">Microsoft.ApplicationInsights (2.21.0)</a>
  </li>
</ol>

NOTE: The version in this project for the dependency (Microsoft.ApplicationInsights) is 2.21.0.

Also, this project can be run locally and successfully demonstrate data ingestion into Application Insights by adding the app setting 'APPINSIGHTS_INSTRUMENTATIONKEY' with a valid instrumentation key value into the project local.settings.json file.
