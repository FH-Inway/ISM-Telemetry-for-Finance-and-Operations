using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;

namespace D365FOAvailabilityCheck;

public static class TelemetryClientSingleton
{
    private static TelemetryClient _telemetryClient = null;
    public static TelemetryClient TelemetryClient()
    {
        if (_telemetryClient == null)
        {
            var telemetryConfiguration = new TelemetryConfiguration()
            {
                ConnectionString = 
                    Environment.GetEnvironmentVariable(
                        "AvailabilityChecks.ApplicationInsightsConnectionString")
            };
            _telemetryClient = new TelemetryClient(telemetryConfiguration);
        }
        return _telemetryClient;
    }
}
