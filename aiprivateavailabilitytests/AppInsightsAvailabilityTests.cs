using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace aiprivateavailabilitytests
{
    public class AppInsightsAvailabilityTests
    {
        public async Task RunAvailabilityTestAsync(ILogger log)
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
