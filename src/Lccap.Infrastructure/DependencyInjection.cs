using Lccap.Application.Common.Interfaces;
using Lccap.Application.ExposureAnalysisJobs.Computation.Python;
using Lccap.Infrastructure.Persistence;
using Lccap.Infrastructure.Persistence.Interceptors;
using Lccap.Infrastructure.Security;
using Lccap.Infrastructure.Storage;
using Lccap.Infrastructure.ExposureAnalysisJobs.Python;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Lccap.Infrastructure;

public static class DependencyInjection
{
    public static void AddInfrastructureServices(IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException(
                "Connection string 'Postgres' is not configured (expected ConnectionStrings:Postgres).");

        _ = services.AddOptions<PythonExposureComputationOptions>()
            .Bind(configuration.GetSection("PythonAi"));

        _ = services.AddOptions<PythonExposureComputationFeatureOptions>()
            .Bind(configuration.GetSection("PythonAi"));

        var pythonBaseUrl = configuration["PythonAi:BaseUrl"];
        _ = services.AddHttpClient<PythonExposureComputationServiceClient>(
            httpClient =>
            {
                httpClient.Timeout = Timeout.InfiniteTimeSpan;

                if (!string.IsNullOrWhiteSpace(pythonBaseUrl)
                    && Uri.TryCreate(pythonBaseUrl, UriKind.Absolute, out var parsedBaseUrl))
                {
                    httpClient.BaseAddress = parsedBaseUrl;
                }
            });

        _ = services.AddScoped<IPythonExposureComputationServiceClient>(serviceProvider =>
            serviceProvider.GetRequiredService<PythonExposureComputationServiceClient>());

        _ = services.AddScoped<AuditSaveChangesInterceptor>();
        _ = services.AddScoped<PasswordHasher>();
        _ = services.AddScoped<JwtTokenGenerator>();
        _ = services.AddScoped<IFileStorageService, LocalFileStorageService>();
        _ = services.AddScoped<Seed.DemoSeedService>();

        _ = services.AddDbContext<LccapDbContext>((serviceProvider, options) =>
        {
            _ = options.UseNpgsql(connectionString).AddInterceptors(
                serviceProvider.GetRequiredService<AuditSaveChangesInterceptor>());
        });
        _ = services.AddScoped<ILccapDbContext>(serviceProvider => serviceProvider.GetRequiredService<LccapDbContext>());
    }
}
