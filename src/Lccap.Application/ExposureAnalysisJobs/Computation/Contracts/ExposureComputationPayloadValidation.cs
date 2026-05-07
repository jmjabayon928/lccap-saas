namespace Lccap.Application.ExposureAnalysisJobs.Computation.Contracts;

public static class ExposureComputationPayloadValidation
{
    public static IReadOnlyList<string> ValidateRequest(ExposureComputationServiceRequest request)
    {
        var errors = new List<string>();

        if (request.JobId == Guid.Empty) errors.Add("JobId is required.");
        if (request.AccountId == Guid.Empty) errors.Add("AccountId is required.");
        if (request.PlanId == Guid.Empty) errors.Add("PlanId is required.");
        if (request.HazardLayerId == Guid.Empty) errors.Add("HazardLayerId is required.");

        if (string.IsNullOrWhiteSpace(request.ComputationVersion))
        {
            errors.Add("ComputationVersion is required.");
        }

        if (request.CrsPolicy is null) errors.Add("CrsPolicy is required.");
        if (request.GeometryPolicy is null) errors.Add("GeometryPolicy is required.");

        if (request.HazardFeatures is null) errors.Add("HazardFeatures is required.");

        if (request.HazardLayer is null)
        {
            errors.Add("HazardLayer metadata is required.");
        }
        else
        {
            if (string.IsNullOrWhiteSpace(request.HazardLayer.HazardType))
            {
                errors.Add("HazardLayer.HazardType is required.");
            }

            if (string.IsNullOrWhiteSpace(request.HazardLayer.Severity))
            {
                errors.Add("HazardLayer.Severity is required.");
            }
        }

        if (request.Barangays is null) errors.Add("Barangays is required.");
        if (request.CriticalFacilities is null) errors.Add("CriticalFacilities is required.");

        return errors;
    }

    public static IReadOnlyList<string> ValidateResponse(ExposureComputationServiceResponse response)
    {
        var errors = new List<string>();

        if (response is null)
        {
            return new[] { "Response is required." };
        }

        if (string.IsNullOrWhiteSpace(response.EngineName))
        {
            errors.Add("EngineName is required.");
        }

        if (response.CompletedAtUtc == default)
        {
            errors.Add("CompletedAtUtc is required.");
        }

        var results = response.Results;
        if (results is null)
        {
            errors.Add("Results is required.");
            results = Array.Empty<ExposureComputationServiceResultRow>();
        }

        if (!response.Success)
        {
            if (string.IsNullOrWhiteSpace(response.ErrorCode)) errors.Add("ErrorCode is required when Success is false.");
            if (string.IsNullOrWhiteSpace(response.ErrorMessage)) errors.Add("ErrorMessage is required when Success is false.");

            if (results.Count != 0)
            {
                errors.Add("Results must be empty when Success is false.");
            }
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(response.ErrorCode)) errors.Add("ErrorCode must be empty when Success is true.");

            foreach (var row in results)
            {
                if (row is null)
                {
                    errors.Add("ResultRow must not be null.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(row.HazardType)) errors.Add("ResultRow.HazardType is required.");

                if (row.ExposedFacilityCount < 0) errors.Add("ResultRow.ExposedFacilityCount must be >= 0.");

                if (row.ExposedAreaHectares != null && row.ExposedAreaHectares < 0)
                {
                    errors.Add("ResultRow.ExposedAreaHectares must be null or >= 0.");
                }

                if (row.ExposedPopulation != null && row.ExposedPopulation < 0)
                {
                    errors.Add("ResultRow.ExposedPopulation must be null or >= 0.");
                }

                if (row.RiskScore != null && row.RiskScore < 0)
                {
                    errors.Add("ResultRow.RiskScore must be null or >= 0.");
                }

                if (row.SummaryJson is null) errors.Add("ResultRow.SummaryJson is required.");
            }
        }

        return errors;
    }
}

