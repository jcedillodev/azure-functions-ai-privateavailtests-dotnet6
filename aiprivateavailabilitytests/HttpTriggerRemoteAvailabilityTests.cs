using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Threading;
using Newtonsoft.Json;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using System.Net.Http;

namespace aiprivateavailabilitytests
{
    public static class HttpTriggerRemoteAvailabilityTests
    {
        private static TelemetryClient telemetryClient;

        [FunctionName("HttpTriggerRemoteAvailabilityTests")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log, Microsoft.Azure.WebJobs.ExecutionContext executionContext)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string name = req.Query["name"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            name = name ?? data?.name;

            if (telemetryClient == null)
            {
                // Initializing a telemetry configuration for Application Insights based on connection string 
                log.LogInformation($"Telemetry configuration initiated for Application Insights at: {DateTime.Now}");

                var telemetryConfiguration = new TelemetryConfiguration();
                telemetryConfiguration.ConnectionString = Environment.GetEnvironmentVariable("AI_PRIVATEAVAILTESTS_APPLICATIONINSIGHTS_CONNECTION_STRING");
                telemetryConfiguration.TelemetryChannel = new InMemoryChannel();
                telemetryClient = new TelemetryClient(telemetryConfiguration);
            }

            string testName = executionContext.FunctionName;
            string location = Environment.GetEnvironmentVariable("APP_REGION");
            var availability = new AvailabilityTelemetry
            {
                Name = testName,
                RunLocation = location,
                Success = false,
            };

            availability.Context.Operation.ParentId = Activity.Current.SpanId.ToString();
            availability.Context.Operation.Id = Activity.Current.RootId;
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            log.LogInformation($"Telemetry configuration stopwatch class initialized at: {DateTime.Now}");

            try
            {
                using (var activity = new Activity("AvailabilityContext"))
                {
                    activity.Start();
                    availability.Id = Activity.Current.SpanId.ToString();
                    // Run business logic 
                    // await RunAvailabilityTestAsync(log);
                    AppInsightsAvailabilityTests appInsightsAvailabilityTests = new AppInsightsAvailabilityTests();
                    await appInsightsAvailabilityTests.RunAvailabilityTestAsync(log);
                }
                availability.Success = true;
            }

            catch (Exception ex)
            {
                availability.Message = ex.Message;
                throw;
            }

            finally
            {
                stopwatch.Stop();
                availability.Duration = stopwatch.Elapsed;
                availability.Timestamp = DateTimeOffset.UtcNow;
                telemetryClient.TrackAvailability(availability);
                telemetryClient.Flush();

                log.LogInformation($"Availability test completed and logged to Application Insights at: {DateTime.Now}");
            }

            string responseMessage = string.IsNullOrEmpty(name)
                ? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
                : $"Hello, {name}. This HTTP triggered function executed successfully.";

            return new OkObjectResult(responseMessage);
        }

        /*public async static Task RunAvailabilityTestAsync(ILogger log)
        {
            using (var httpClient = new HttpClient())
            {
                var appendpoint = string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AI_PRIVATEAVAILTESTS_APP_ENDPOINT")) ? "https://www.google.com/" : Environment.GetEnvironmentVariable("AI_PRIVATEAVAILTESTS_APP_ENDPOINT");

                // TODO: Replace with your business logic 
                try
                {
                    log.LogInformation($"Attempting to initiate async http call to app endpoint at: {DateTime.Now}");

                    await httpClient.GetStringAsync(appendpoint);
                }
                catch (Exception ex)
                {
                    log.LogInformation(ex.Message);
                }
            }
        }*/
    }
}
