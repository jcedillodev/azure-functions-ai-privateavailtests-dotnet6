using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Logging;

namespace aiprivateavailabilitytests
{
    public class TimerTriggerRemoteAvailabilityTests
    {
        private static TelemetryClient telemetryClient;

        [FunctionName("TimerTriggerRemoteAvailabilityTests")]
        public async Task RunAsync([TimerTrigger("%TIMER_SCHEDULE%")] TimerInfo myTimer, ILogger log, Microsoft.Azure.WebJobs.ExecutionContext executionContext)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            if (telemetryClient == null)
            {
                // Initializing a telemetry configuration for Application Insights based on connection string 
                log.LogInformation($"Telemetry configuration initiated for Application Insights at: {DateTime.Now}");

                var telemetryConfiguration = new TelemetryConfiguration();
                telemetryConfiguration.ConnectionString = Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING");
                telemetryConfiguration.TelemetryChannel = new InMemoryChannel();
                telemetryClient = new TelemetryClient(telemetryConfiguration);
            }

            string testName = executionContext.FunctionName;
            string location = Environment.GetEnvironmentVariable("REGION_NAME");
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
                    await RunAvailabilityTestAsync(log);
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
        }

        public async static Task RunAvailabilityTestAsync(ILogger log)
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
        }
    }
}
