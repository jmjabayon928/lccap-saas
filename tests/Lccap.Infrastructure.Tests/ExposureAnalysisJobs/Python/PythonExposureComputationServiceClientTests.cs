using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Lccap.Application.ExposureAnalysisJobs.Computation.Contracts;
using Lccap.Application.ExposureAnalysisJobs.Computation.Python;
using Lccap.Infrastructure.ExposureAnalysisJobs.Python;
using Microsoft.Extensions.Options;
using Xunit;

namespace Lccap.Infrastructure.Tests.ExposureAnalysisJobs.Python;

public sealed class PythonExposureComputationServiceClientTests
{
    private static HttpClient CreateHttpClient(HttpMessageHandler handler)
    {
        return new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost")
        };
    }

    private static ExposureComputationServiceRequest CreateRequest()
    {
        var hazardFeaturesJson = JsonDocument.Parse(
            "{\"type\":\"FeatureCollection\",\"features\":[]}");

        return new ExposureComputationServiceRequest(
            JobId: Guid.NewGuid(),
            AccountId: Guid.NewGuid(),
            PlanId: Guid.NewGuid(),
            HazardLayerId: Guid.NewGuid(),
            Mode: null,
            RequestedAtUtc: null,
            RequestedByUserId: null,
            ComputationVersion: ExposureComputationContractVersion.Current,
            CrsPolicy: ExposureComputationCrsPolicy.RequireExplicitEpsg4326(),
            GeometryPolicy: ExposureComputationGeometryPolicy.FailFastNoRepair(),
            HazardLayer: new ExposureComputationHazardLayer(
                Id: Guid.NewGuid(),
                HazardType: "River",
                Severity: "High",
                MapAssetId: null),
            HazardFeatures: hazardFeaturesJson,
            Barangays: Array.Empty<ExposureComputationBarangay>(),
            CriticalFacilities: Array.Empty<ExposureComputationCriticalFacility>());
    }

    [Fact]
    public async Task ExecuteAsync_maps_engine_unavailable_response()
    {
        var handler = new StubHttpMessageHandler(async (_, _) =>
        {
            var json = """
            {
              "success": false,
              "engineName": "ExposureComputationScaffold",
              "engineVersion": "scaffold",
              "computationRunId": null,
              "completedAtUtc": "2026-05-06T00:00:00Z",
              "errorCode": "EngineUnavailable",
              "errorMessage": "Exposure computation engine is not configured.",
              "diagnostics": {
                "message": "Computation endpoint is scaffolded only.",
                "warnings": [],
                "validationNotes": [],
                "geometryFeatureCount": null,
                "barangayCount": null,
                "criticalFacilityCount": null,
                "crsDescription": null
              },
              "results": []
            }
            """;

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            return await Task.FromResult(response);
        });

        var httpClient = CreateHttpClient(handler);
        var options = Options.Create(new PythonExposureComputationOptions());
        var client = new PythonExposureComputationServiceClient(httpClient, options);

        var result = await client.ExecuteAsync(CreateRequest(), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(ExposureComputationEngineErrorCode.EngineUnavailable, result.ErrorCode);
        Assert.Equal("Exposure computation engine is not configured.", result.ErrorMessage);
        Assert.Empty(result.Results);
    }

    [Fact]
    public async Task ExecuteAsync_returns_engine_unavailable_when_http_request_fails()
    {
        var handler = new StubHttpMessageHandler((_, _) =>
            Task.FromException<HttpResponseMessage>(new HttpRequestException("boom")));

        var httpClient = CreateHttpClient(handler);
        var options = Options.Create(new PythonExposureComputationOptions());
        var client = new PythonExposureComputationServiceClient(httpClient, options);

        var result = await client.ExecuteAsync(CreateRequest(), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(ExposureComputationEngineErrorCode.EngineUnavailable, result.ErrorCode);
        Assert.Equal("Exposure computation engine is not configured.", result.ErrorMessage);
        Assert.Empty(result.Results);
        Assert.Null(result.EngineVersion);
        Assert.Null(result.ComputationRunId);
    }

    [Fact]
    public async Task ExecuteAsync_returns_engine_unavailable_when_response_is_non_success_status()
    {
        var handler = new StubHttpMessageHandler(async (_, _) =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            };
            return await Task.FromResult(response);
        });

        var httpClient = CreateHttpClient(handler);
        var options = Options.Create(new PythonExposureComputationOptions());
        var client = new PythonExposureComputationServiceClient(httpClient, options);

        var result = await client.ExecuteAsync(CreateRequest(), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(ExposureComputationEngineErrorCode.EngineUnavailable, result.ErrorCode);
        Assert.Equal("Exposure computation engine is not configured.", result.ErrorMessage);
        Assert.Empty(result.Results);
        Assert.Null(result.EngineVersion);
        Assert.Null(result.ComputationRunId);
    }

    [Fact]
    public async Task ExecuteAsync_returns_validation_failed_when_response_contract_is_invalid()
    {
        var handler = new StubHttpMessageHandler(async (_, _) =>
        {
            // Invalid contract: success=false but results is not empty.
            var json = """
            {
              "success": false,
              "engineName": "ExposureComputationScaffold",
              "engineVersion": "scaffold",
              "computationRunId": null,
              "completedAtUtc": "2026-05-06T00:00:00Z",
              "errorCode": "EngineUnavailable",
              "errorMessage": "Exposure computation engine is not configured.",
              "diagnostics": {
                "message": "Computation endpoint is scaffolded only.",
                "warnings": [],
                "validationNotes": [],
                "geometryFeatureCount": null,
                "barangayCount": null,
                "criticalFacilityCount": null,
                "crsDescription": null
              },
              "results": [
                {
                  "barangayId": null,
                  "criticalFacilityId": null,
                  "hazardLayerId": null,
                  "hazardType": "River",
                  "severity": "High",
                  "exposedAreaHectares": null,
                  "exposedFacilityCount": 0,
                  "exposedPopulation": null,
                  "riskScore": null,
                  "summaryJson": {}
                }
              ]
            }
            """;

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            return await Task.FromResult(response);
        });

        var httpClient = CreateHttpClient(handler);
        var options = Options.Create(new PythonExposureComputationOptions());
        var client = new PythonExposureComputationServiceClient(httpClient, options);

        var result = await client.ExecuteAsync(CreateRequest(), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(ExposureComputationEngineErrorCode.ValidationFailed, result.ErrorCode);
        Assert.Equal("Exposure computation service returned an invalid response.", result.ErrorMessage);
        Assert.Empty(result.Results);
        Assert.Null(result.EngineVersion);
        Assert.Null(result.ComputationRunId);
    }

    [Fact]
    public async Task ExecuteAsync_deserializes_and_validates_success_response_with_facility_result_row()
    {
        var completedAtUtc = DateTimeOffset.Parse("2026-05-06T00:00:00Z");

        var json = $@"{{
  ""success"": true,
  ""engineName"": ""FacilityExposureEngine"",
  ""engineVersion"": ""facility-v1"",
  ""computationRunId"": null,
  ""completedAtUtc"": ""{completedAtUtc:O}"",
  ""errorCode"": null,
  ""errorMessage"": null,
  ""diagnostics"": {{
    ""message"": ""Facility-only point-in-polygon computation completed."",
    ""warnings"": [],
    ""validationNotes"": [""exposedAreaHectares, exposedPopulation, and riskScore are deferred.""],
    ""geometryFeatureCount"": 1,
    ""barangayCount"": 1,
    ""criticalFacilityCount"": 1,
    ""crsDescription"": ""EPSG:4326 (Explicit)""
  }},
  ""results"": [
    {{
      ""barangayId"": ""00000000-0000-0000-0000-000000000001"",
      ""criticalFacilityId"": ""00000000-0000-0000-0000-000000000002"",
      ""hazardLayerId"": ""00000000-0000-0000-0000-000000000003"",
      ""hazardType"": ""Flood"",
      ""severity"": ""High"",
      ""exposedAreaHectares"": null,
      ""exposedFacilityCount"": 1,
      ""exposedPopulation"": null,
      ""riskScore"": null,
      ""summaryJson"": {{
        ""mode"": ""FacilityOnlyPointInPolygon"",
        ""boundaryPolicy"": ""BoundaryInclusive"",
        ""matchedHazardFeatureIds"": [""hazard-1""]
      }}
    }}
  ]
}}";

        var handler = new StubHttpMessageHandler(async (_, _) =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            return await Task.FromResult(response);
        });

        var httpClient = CreateHttpClient(handler);
        var options = Options.Create(new PythonExposureComputationOptions());
        var client = new PythonExposureComputationServiceClient(httpClient, options);

        var result = await client.ExecuteAsync(CreateRequest(), CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal("FacilityExposureEngine", result.EngineName);
        Assert.Equal("facility-v1", result.EngineVersion);
        Assert.Single(result.Results);

        var row = result.Results[0];
        Assert.Equal(1, row.ExposedFacilityCount);
        Assert.Null(row.ExposedAreaHectares);
        Assert.Null(row.ExposedPopulation);
        Assert.Null(row.RiskScore);
        Assert.NotNull(row.SummaryJson);
        Assert.Equal(
            "FacilityOnlyPointInPolygon",
            row.SummaryJson.RootElement.GetProperty("mode").GetString());
    }

    [Fact]
    public async Task ExecuteAsync_returns_validation_failed_when_success_response_has_invalid_result_row()
    {
        var completedAtUtc = DateTimeOffset.Parse("2026-05-06T00:00:00Z");

        var json = $@"{{
  ""success"": true,
  ""engineName"": ""FacilityExposureEngine"",
  ""engineVersion"": ""facility-v1"",
  ""computationRunId"": null,
  ""completedAtUtc"": ""{completedAtUtc:O}"",
  ""errorCode"": null,
  ""errorMessage"": null,
  ""diagnostics"": {{
    ""message"": ""Facility-only point-in-polygon computation completed."",
    ""warnings"": [],
    ""validationNotes"": [""exposedAreaHectares, exposedPopulation, and riskScore are deferred.""],
    ""geometryFeatureCount"": 1,
    ""barangayCount"": 1,
    ""criticalFacilityCount"": 1,
    ""crsDescription"": ""EPSG:4326 (Explicit)""
  }},
  ""results"": [
    {{
      ""barangayId"": ""00000000-0000-0000-0000-000000000001"",
      ""criticalFacilityId"": ""00000000-0000-0000-0000-000000000002"",
      ""hazardLayerId"": ""00000000-0000-0000-0000-000000000003"",
      ""hazardType"": ""Flood"",
      ""severity"": ""High"",
      ""exposedAreaHectares"": null,
      ""exposedFacilityCount"": -1,
      ""exposedPopulation"": null,
      ""riskScore"": null,
      ""summaryJson"": {{
        ""mode"": ""FacilityOnlyPointInPolygon"",
        ""boundaryPolicy"": ""BoundaryInclusive"",
        ""matchedHazardFeatureIds"": [""hazard-1""]
      }}
    }}
  ]
}}";

        var handler = new StubHttpMessageHandler(async (_, _) =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            return await Task.FromResult(response);
        });

        var httpClient = CreateHttpClient(handler);
        var options = Options.Create(new PythonExposureComputationOptions());
        var client = new PythonExposureComputationServiceClient(httpClient, options);

        var result = await client.ExecuteAsync(CreateRequest(), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(ExposureComputationEngineErrorCode.ValidationFailed, result.ErrorCode);
        Assert.Equal("Exposure computation service returned an invalid response.", result.ErrorMessage);
        Assert.Empty(result.Results);
        Assert.Null(result.EngineVersion);
        Assert.Null(result.ComputationRunId);
    }

    [Fact]
    public async Task ExecuteAsync_returns_engine_unavailable_when_json_is_invalid()
    {
        var handler = new StubHttpMessageHandler(async (_, _) =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{", Encoding.UTF8, "application/json")
            };
            return await Task.FromResult(response);
        });

        var httpClient = CreateHttpClient(handler);
        var options = Options.Create(new PythonExposureComputationOptions());
        var client = new PythonExposureComputationServiceClient(httpClient, options);

        var result = await client.ExecuteAsync(CreateRequest(), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(ExposureComputationEngineErrorCode.EngineUnavailable, result.ErrorCode);
        Assert.Equal("Exposure computation engine is not configured.", result.ErrorMessage);
        Assert.Empty(result.Results);
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;

        public StubHttpMessageHandler(
            Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
        {
            _handler = handler;
        }

        public StubHttpMessageHandler(
            Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> handler)
        {
            _handler = (request, token) => Task.FromResult(handler(request, token));
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return _handler(request, cancellationToken);
        }
    }
}

