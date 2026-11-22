using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.Dynamics.Integration.Telemetry.AppInsights;

namespace ISMTelemetry
{
    /// <summary>
    /// Owns the process-local collector loop.
    /// </summary>
    public static class TelemetryCollectorHost
    {
        private static readonly object SyncRoot = new object();
        private static Timer collectorTimer;
        private static string collectorInstanceId;
        private static TelemetryClient telemetryClient;
        private static readonly TimeSpan SamplingInterval = TimeSpan.FromMinutes(1);

        /// <summary>
        /// Starts collection once for the current application domain.
        /// </summary>
        /// <param name="telemetryClient">
        /// The configured Dynamics Application Insights telemetry client.
        /// </param>
        public static void EnsureStarted(TelemetryClient client)
        {
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            lock (SyncRoot)
            {
                if (collectorTimer != null)
                {
                    return;
                }

                collectorInstanceId = Guid.NewGuid().ToString("D");
                telemetryClient = client;
                collectorTimer = new Timer(
                    Collect,
                    null,
                    Timeout.InfiniteTimeSpan,
                    Timeout.InfiniteTimeSpan);

                collectorTimer.Change(TimeSpan.Zero, Timeout.InfiniteTimeSpan);
            }
        }

        private static void Collect(object state)
        {
            try
            {
                using (Process process = Process.GetCurrentProcess())
                {
                    string processId = process.Id.ToString();
                    string machineName = Environment.MachineName;
                    string processStartTime = process.StartTime.ToUniversalTime().ToString("o");

                    TrackMetric(
                        "ISM.Process.WorkingSetBytes",
                        process.WorkingSet64,
                        processId,
                        machineName,
                        processStartTime);
                    TrackMetric(
                        "ISM.Process.PrivateMemoryBytes",
                        process.PrivateMemorySize64,
                        processId,
                        machineName,
                        processStartTime);
                    TrackMetric(
                        "ISM.Process.ManagedMemoryBytes",
                        GC.GetTotalMemory(false),
                        processId,
                        machineName,
                        processStartTime);

                    telemetryClient.FlushClient();
                }
            }
            catch (Exception exception)
            {
                string failure = String.Format(
                    "ISM telemetry collection failed for instance {0}: {1}",
                    collectorInstanceId,
                    exception);
                Trace.TraceError(failure);
            }
            finally
            {
                Timer timer = collectorTimer;
                if (timer != null)
                {
                    timer.Change(SamplingInterval, Timeout.InfiniteTimeSpan);
                }
            }
        }

        private static void TrackMetric(
            string metricName,
            double value,
            string processId,
            string machineName,
            string processStartTime)
        {
            telemetryClient.TrackMetricWithDimensions(
                metricName,
                value,
                "ProcessId",
                processId,
                "MachineName",
                machineName,
                "ProcessStartTimeUtc",
                processStartTime);
        }
    }
}
