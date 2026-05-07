using System.Net.Http.Json;
using System.Text.Json;
using Lccap.Application.ExposureAnalysisJobs.Computation.Contracts;
using Lccap.Application.ExposureAnalysisJobs.Computation.Python;
using Microsoft.Extensions.Options;

namespace Lccap.Infrastructure.ExposureAnalysisJobs.Python;

public sealed class PythonExposureComputationServiceClient : IPythonExposureComputationServiceClient
{
    private const string PythonEngineName = "PythonHttpClient";

    private static readonly JsonSerializerOptions CamelCaseJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly HttpClient _httpClient;
    private readonly PythonExposureComputationOptions _options;

    public PythonExposureComputationServiceClient(
        HttpClient httpClient,
        IOptions<PythonExposureComputationOptions> options)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(options);

        _httpClient = httpClient;
        _options = options.Value;

        if (_options.TimeoutSeconds > 0 && _httpClient.Timeout == Timeout.InfiniteTimeSpan)
        {
            _httpClient.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);
        }
    }

    public async Task<ExposureComputationServiceResponse> ExecuteAsync(
        ExposureComputationServiceRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            using var httpResponse = await _httpClient.PostAsJsonAsync(
                    requestUri: _options.ComputePath,
                    value: request,
                    options: CamelCaseJsonOptions,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            if (!httpResponse.IsSuccessStatusCode)
            {
                return EngineUnavailableResponse();
            }

            var parsed = await httpResponse.Content.ReadFromJsonAsync<ExposureComputationServiceResponse>(
                    CamelCaseJsonOptions,
                    cancellationToken)
                .ConfigureAwait(false);

            if (parsed is null)
            {
                return EngineUnavailableResponse();
            }

            var validationErrors = ExposureComputationPayloadValidation.ValidateResponse(parsed);
            if (validationErrors.Count != 0)
            {
                return ValidationFailedResponse(validationErrors);
            }

            return parsed;
        }
        catch (HttpRequestException)
        {
            return EngineUnavailableResponse();
        }
        catch (TaskCanceledException)
        {
            return EngineUnavailableResponse();
        }
        catch (JsonException)
        {
            return EngineUnavailableResponse();
        }
        catch (NotSupportedException)
        {
            return EngineUnavailableResponse();
        }
    }

    private static ExposureComputationServiceResponse EngineUnavailableResponse()
    {
        var nowUtc = DateTimeOffset.UtcNow;

        return new ExposureComputationServiceResponse(
            Success: false,
            EngineName: PythonEngineName,
            EngineVersion: null,
            ComputationRunId: null,
            CompletedAtUtc: nowUtc,
            ErrorCode: ExposureComputationEngineErrorCode.EngineUnavailable,
            ErrorMessage: "Exposure computation engine is not configured.",
            Diagnostics: new ExposureComputationDiagnostics(
                Message: "Exposure computation engine is not reachable.",
                Warnings: Array.Empty<string>(),
                ValidationNotes: Array.Empty<string>(),
                GeometryFeatureCount: null,
                BarangayCount: null,
                CriticalFacilityCount: null,
                CrsDescription: null),
            Results: Array.Empty<ExposureComputationServiceResultRow>());
    }

    private static ExposureComputationServiceResponse ValidationFailedResponse(IReadOnlyList<string> validationErrors)
    {
        var nowUtc = DateTimeOffset.UtcNow;

        var notes = validationErrors is { Count: > 0 }
            ? validationErrors
            : Array.Empty<string>();

        return new ExposureComputationServiceResponse(
            Success: false,
            EngineName: PythonEngineName,
            EngineVersion: null,
            ComputationRunId: null,
            CompletedAtUtc: nowUtc,
            ErrorCode: ExposureComputationEngineErrorCode.ValidationFailed,
            ErrorMessage: "Exposure computation service returned an invalid response.",
            Diagnostics: new ExposureComputationDiagnostics(
                Message: "Exposure computation service returned an invalid response contract.",
                Warnings: Array.Empty<string>(),
                ValidationNotes: notes,
                GeometryFeatureCount: null,
                BarangayCount: null,
                CriticalFacilityCount: null,
                CrsDescription: null),
            Results: Array.Empty<ExposureComputationServiceResultRow>());
    }
}

