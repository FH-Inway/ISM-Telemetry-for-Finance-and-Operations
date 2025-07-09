using D365FOAvailabilityCheck.Checks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace D365FOAvailabilityCheck.Functions;

public class GetLegalEntitiesAvailabilityCheckFunction : BaseAvailabilityCheckFunction
{
    private readonly ILogger _logger;

    public GetLegalEntitiesAvailabilityCheckFunction(ILogger<GetLegalEntitiesAvailabilityCheckFunction> logger)
    {
        _logger = logger;
    }

    [Function(nameof(CheckGetLegalEntitiesAvailability))]
    public async Task CheckGetLegalEntitiesAvailability(
        [TimerTrigger("%AvailabilityChecks.GetLegalEntitiesCheckSchedule%", RunOnStartup = true)]
        TimerInfo myTimer)
    {
        var testName = "Check getting legal entities via OData availability";
        await ExecuteAvailabilityTest(
            new GetLegalEntitiesCheck(),
            testName,
            _logger);
    }
}
