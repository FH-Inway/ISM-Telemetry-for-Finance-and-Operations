using D365FOAvailabilityCheck.Checks;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace D365FOAvailabilityCheck.Functions;

public class BaseAvailabilityCheckFunction
{
    private AvailabilityTelemetry GetAvailabilityTelemetry(string availabilityName)
    {
        string location = Environment.GetEnvironmentVariable("AvailabilityChecks.RegionName");

        var availability = new AvailabilityTelemetry
        {
            Name = availabilityName,
            RunLocation = location,
            Success = false
        };
        return availability;
    }

    public async Task ExecuteAvailabilityTest(
        BaseD365FOAPICheck D365FOAPICheck,
        string testName,
        ILogger log)
    {
        Stopwatch stopwatch = new Stopwatch();
        AvailabilityTelemetry availability = GetAvailabilityTelemetry(testName);

        try
        {
            using var activity = new Activity("AvailabilityContext");
            stopwatch.Start();
            activity.Start();
            availability.Context.Operation.Id = Activity.Current.RootId;
            await D365FOAPICheck.RunCheck(
                TelemetryClientSingleton.TelemetryClient(),
                availability.Id,
                Activity.Current.RootId);
            stopwatch.Stop();
            availability.Success = true;
            log.LogInformation(
                $"{testName} - Availability Check Run completed successfully at: {DateTime.Now}");
        }
        catch (Exception ex)
        {
            log.LogInformation($"{testName} - Availability Check Run failed at: {DateTime.Now}");
            availability.Success = false;
            availability.Message = ex.Message;
            throw;
        }
        finally
        {
            availability.Duration = stopwatch.Elapsed;
            availability.Timestamp = DateTimeOffset.UtcNow;
            TelemetryClientSingleton.TelemetryClient().TrackAvailability(availability);
            TelemetryClientSingleton.TelemetryClient().Flush();
        }
    }
}
