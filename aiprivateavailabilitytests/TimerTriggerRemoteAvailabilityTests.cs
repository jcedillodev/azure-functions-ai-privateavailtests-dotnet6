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

            // Check if there is an existing telemetry client; if not, then establish one to the target app insights instance
            // using the app setting (AI_PRIVATEAVAILTESTS_APPLICATIONINSIGHTS_CONNECTION_STRING)
            if (telemetryClient == null)
            {
                // Initializing a telemetry configuration class (Azure Monitor SDK) for Application Insights based on connection string 
                log.LogInformation($"Telemetry configuration initiated for Application Insights at: {DateTime.Now}");

                var telemetryConfiguration = new TelemetryConfiguration();
                telemetryConfiguration.ConnectionString = Environment.GetEnvironmentVariable("AI_PRIVATEAVAILTESTS_APPLICATIONINSIGHTS_CONNECTION_STRING");
                telemetryConfiguration.TelemetryChannel = new InMemoryChannel();
                telemetryClient = new TelemetryClient(telemetryConfiguration);
            }

            // Retrieve values such as the function name (executionContext - HttpTrigger attribute)
            // along with the target app(s) region from the app setting (APP_REGION)
            string fctName = executionContext.FunctionName;
            string location = Environment.GetEnvironmentVariable("APP_REGION");

            // Instantiate the AvailabilityTelemetry class (Azure Monitor SDK) and set property values
            var availability = new AvailabilityTelemetry
            {
                Name = fctName,
                RunLocation = location,
                Success = false,
            };

            availability.Context.Operation.ParentId = Activity.Current.SpanId.ToString();
            availability.Context.Operation.Id = Activity.Current.RootId;

            // Instantiate the StopWatch class (Azure Monitor SDK) to track start/stop times from when call is made to target app(s) for duration
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            log.LogInformation($"Telemetry configuration stopwatch class initialized at: {DateTime.Now}");

            try
            {
                using (var activity = new Activity("AvailabilityContext"))
                {
                    activity.Start();
                    availability.Id = Activity.Current.SpanId.ToString();

                    // Instantiate the AppInsightsAvailabilityTests
                    AppInsightsAvailabilityTests appInsightsAvailabilityTests = new AppInsightsAvailabilityTests();
                    await appInsightsAvailabilityTests.RunAvailabilityTestAsync(log);
                }
                availability.Success = true;
            }

            catch (Exception ex)
            {
                // If errors occur within the try/catch block, log error(s)
                availability.Message = ex.Message;
                log.LogInformation(availability.Message);
                throw;
            }

            finally
            {
                // Stop StopWatch instance run then set duration/timestamp properties for Availablility object
                stopwatch.Stop();
                availability.Duration = stopwatch.Elapsed;
                availability.Timestamp = DateTimeOffset.UtcNow;

                // Using the telemetry client, log data from Availability object into target app insights
                telemetryClient.TrackAvailability(availability);

                // Remove any existing in-memory data from the telemetry client before finishing execution
                telemetryClient.Flush();

                log.LogInformation($"Availability test completed and logged to Application Insights at: {DateTime.Now}");
            }
        }
    }
}
