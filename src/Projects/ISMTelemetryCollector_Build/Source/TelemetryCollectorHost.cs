using System;
using System.Diagnostics;
using System.Threading;

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
        private static readonly TimeSpan SamplingInterval = TimeSpan.FromMinutes(1);

        /// <summary>
        /// Starts collection once for the current application domain.
        /// </summary>
        public static void EnsureStarted()
        {
            lock (SyncRoot)
            {
                if (collectorTimer != null)
                {
                    return;
                }

                collectorInstanceId = Guid.NewGuid().ToString("D");
                collectorTimer = new Timer(
                    Collect,
                    null,
                    Timeout.InfiniteTimeSpan,
                    Timeout.InfiniteTimeSpan);

                Process process = Process.GetCurrentProcess();
                TelemetryCollectorEventSource.Log.CollectorStarted(
                    process.Id,
                    Environment.MachineName,
                    collectorInstanceId,
                    AppDomain.CurrentDomain.FriendlyName);

                collectorTimer.Change(TimeSpan.Zero, Timeout.InfiniteTimeSpan);
            }
        }

        private static void Collect(object state)
        {
            try
            {
                using (Process process = Process.GetCurrentProcess())
                {
                    TelemetryCollectorEventSource.Log.SampleCollected(
                        process.Id,
                        process.StartTime.ToUniversalTime().Ticks,
                        collectorInstanceId,
                        Environment.MachineName,
                        AppDomain.CurrentDomain.FriendlyName,
                        process.WorkingSet64,
                        process.PrivateMemorySize64,
                        GC.GetTotalMemory(false));
                }
            }
            catch (Exception exception)
            {
                string failure = String.Format(
                    "ISM telemetry collection failed for instance {0}: {1}",
                    collectorInstanceId,
                    exception);
                Trace.TraceError(failure);

                try
                {
                    TelemetryCollectorEventSource.Log.SamplingFailed(
                        collectorInstanceId,
                        exception.ToString());
                }
                catch (Exception eventSourceException)
                {
                    Trace.TraceError(
                        "ISM telemetry error reporting failed: {0}",
                        eventSourceException);
                }
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
    }
}
