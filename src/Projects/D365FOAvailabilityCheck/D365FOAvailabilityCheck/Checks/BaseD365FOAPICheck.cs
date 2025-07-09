using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using System.Diagnostics;
using System.Net;
using System.Text.Json;

namespace D365FOAvailabilityCheck.Checks;

public abstract class BaseD365FOAPICheck
{
    private const string GrantType = "client_credentials";

    private readonly string _identityServerAuthorizeEndpoint =
        Environment.GetEnvironmentVariable("AvailabilityChecks.IdentityServerAuthorizeEndpoint");

    public abstract Task RunCheck(
        TelemetryClient telemetryClient,
        string parentId,
        string operationId);

    protected void ValidateAPIResponse(
        string expectedAPIResponseContent,
        string actualAPIResponseContents)
    {
        if (!actualAPIResponseContents.Contains(expectedAPIResponseContent))
        {
            throw new Exception(
                $"Response did not contain expected string: {expectedAPIResponseContent}");
        }
    }

    public async Task<string> GetBearerTokenAsync(
        TelemetryClient telemetryClient,
        string parentId,
        string operationId)
    {
        string clientid =
            Environment.GetEnvironmentVariable("AvailabilityChecks.IdentityServerClientId");
        string clientsecret =
            Environment.GetEnvironmentVariable("AvailabilityChecks.IdentityServerClientSecret");
        string resource =
            Environment.GetEnvironmentVariable("AvailabilityChecks.BaseURL");

        using var httpClient = new HttpClient();
        Dictionary<string, string> tokenEndpointParameters =
            new Dictionary<string, string>
            {
                    {"grant_type", GrantType},
                    {"client_id", clientid},
                    {"client_secret", clientsecret},
                    {"resource", resource},
            };

        var tokenEndpointEncodedContent = new FormUrlEncodedContent(tokenEndpointParameters);

        var stopWatch = new Stopwatch();
        stopWatch.Start();

        var tokenResponse = await httpClient
            .PostAsync(_identityServerAuthorizeEndpoint, tokenEndpointEncodedContent);

        stopWatch.Stop();

        RequestTelemetry rt = new RequestTelemetry
        {
            Name = "POST " + _identityServerAuthorizeEndpoint,
            Url = new Uri(_identityServerAuthorizeEndpoint),
            Duration = stopWatch.Elapsed,
            Timestamp = DateTimeOffset.UtcNow,
            ResponseCode = ((int)tokenResponse.StatusCode).ToString(),
            Success = tokenResponse.IsSuccessStatusCode,
        };

        foreach (var responseHeader in tokenResponse.Headers)
        {
            rt.Properties.Add($"{responseHeader.Key}", $"{string.Join(",", responseHeader.Value)}");
        }

        rt.Properties.Add("Response Body", await tokenResponse.Content.ReadAsStringAsync());
        rt.Context.Operation.Id = operationId;
        rt.Context.Operation.ParentId = parentId;
        telemetryClient.TrackRequest(rt);

        if (tokenResponse.StatusCode == HttpStatusCode.OK)
        {
            var tokenResponseContents = await tokenResponse.Content.ReadAsStringAsync();
            using var document = JsonDocument.Parse(tokenResponseContents);
            var tokenResponseJson = document.RootElement;
            var bearerToken = tokenResponseJson.GetProperty("access_token").GetString();
            return bearerToken;
        }

        throw new Exception(
            @"Unable to obtain bearer token from Identity Server for availability test.");
    }

    protected async Task<HttpResponseMessage> ExecuteHttpGetRequest(
        TelemetryClient telemetryClient,
        string parentId,
        string operationId,
        HttpClient httpClient,
        string urlEndpoint)
    {
        var stopWatch = new Stopwatch();
        stopWatch.Start();

        var getResponse = await httpClient.GetAsync(urlEndpoint);

        stopWatch.Stop();

        RequestTelemetry rt = new RequestTelemetry
        {
            Name = "GET " + urlEndpoint,
            Url = new Uri(urlEndpoint),
            Duration = stopWatch.Elapsed,
            Timestamp = DateTimeOffset.UtcNow,
            ResponseCode = ((int)getResponse.StatusCode).ToString(),
            Success = getResponse.IsSuccessStatusCode,
        };

        foreach (var responseHeader in getResponse.Headers)
        {
            rt.Properties.Add($"{responseHeader.Key}", $"{string.Join(",", responseHeader.Value)}");
        }

        rt.Properties.Add("Response Body", await getResponse.Content.ReadAsStringAsync());
        rt.Context.Operation.Id = operationId;
        rt.Context.Operation.ParentId = parentId;
        telemetryClient.TrackRequest(rt);
        return getResponse;
    }

    protected async Task<HttpResponseMessage> ExecuteHttpPostRequest(
        TelemetryClient telemetryClient,
        string parentId,
        string operationId,
        HttpClient httpClient,
        string urlEndpoint,
        HttpContent httpContentParameter = null)
    {
        var stopWatch = new Stopwatch();
        stopWatch.Start();

        var response =
            await httpClient.PostAsync(urlEndpoint, httpContentParameter);

        stopWatch.Stop();

        RequestTelemetry rt = new RequestTelemetry
        {
            Name = "POST " + urlEndpoint,
            Url = new Uri(urlEndpoint),
            Duration = stopWatch.Elapsed,
            Timestamp = DateTimeOffset.UtcNow,
            ResponseCode = ((int)response.StatusCode).ToString(),
            Success = response.IsSuccessStatusCode,
        };

        foreach (var responseHeader in response.Headers)
        {
            rt.Properties.Add($"{responseHeader.Key}", $"{string.Join(",", responseHeader.Value)}");
        }

        rt.Properties.Add("Response Body", await response.Content.ReadAsStringAsync());
        rt.Context.Operation.Id = operationId;
        rt.Context.Operation.ParentId = parentId;
        telemetryClient.TrackRequest(rt);
        return response;
    }
}
