using Microsoft.ApplicationInsights;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace D365FOAvailabilityCheck.Checks;

class GetLegalEntitiesCheck : BaseD365FOAPICheck
{
    public override async Task RunCheck(
        TelemetryClient telemetryClient, 
        string parentId, 
        string operationId)
    {
        var bearerToken = await GetBearerTokenAsync(telemetryClient, parentId, operationId);

        using var httpClient = new HttpClient();

        string baseUrl = Environment.GetEnvironmentVariable("AvailabilityChecks.BaseURL");
        string apiPath = Environment.GetEnvironmentVariable("AvailabilityChecks.GetLegalEntitiesEndpoint");
        string endpoint = $"{baseUrl}{apiPath}";

        httpClient.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", bearerToken);

        var apiResponse = 
            await ExecuteHttpGetRequest(
                telemetryClient, 
                parentId, 
                operationId, 
                httpClient, 
                endpoint);

        var apiResponseContents = await apiResponse.Content.ReadAsStringAsync();

        ValidateAPIResponse(
            Environment.GetEnvironmentVariable("AvailabilityChecks.GetLegalEntitiesExpectedResponse"),
            apiResponseContents);
    }
}
