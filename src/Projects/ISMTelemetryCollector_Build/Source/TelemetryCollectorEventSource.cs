using System.Diagnostics.Tracing;

namespace ISMTelemetry
{
    [EventSource(Name = "ISM-Telemetry-Collector")]
    internal sealed class TelemetryCollectorEventSource : EventSource
    {
        internal static readonly TelemetryCollectorEventSource Log =
            new TelemetryCollectorEventSource();

        [Event(1, Level = EventLevel.Informational, Message = "Collector started.")]
        internal void CollectorStarted(
            int processId,
            string machineName,
            string collectorInstanceId,
            string appDomainName)
        {
            WriteEvent(1, processId, machineName, collectorInstanceId, appDomainName);
        }

        [Event(2, Level = EventLevel.Informational, Message = "Collector sample.")]
        internal void SampleCollected(
            int processId,
            long processStartTimeUtcTicks,
            string collectorInstanceId,
            string machineName,
            string appDomainName,
            long workingSetBytes,
            long privateMemoryBytes,
            long managedMemoryBytes)
        {
            WriteEvent(
                2,
                processId,
                processStartTimeUtcTicks,
                collectorInstanceId,
                machineName,
                appDomainName,
                workingSetBytes,
                privateMemoryBytes,
                managedMemoryBytes);
        }

        [Event(3, Level = EventLevel.Error, Message = "Collector sampling failed.")]
        internal void SamplingFailed(
            string collectorInstanceId,
            string error)
        {
            WriteEvent(3, collectorInstanceId, error);
        }
    }
}
