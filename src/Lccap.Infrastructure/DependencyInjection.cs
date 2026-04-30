using Lccap.Application.Common.Interfaces;
using Lccap.Infrastructure.Persistence;
using Lccap.Infrastructure.Persistence.Interceptors;
using Lccap.Infrastructure.Security;
using Lccap.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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

        _ = services.AddScoped<AuditSaveChangesInterceptor>();
        _ = services.AddScoped<PasswordHasher>();
        _ = services.AddScoped<JwtTokenGenerator>();
        _ = services.AddScoped<IFileStorageService, LocalFileStorageService>();

        _ = services.AddDbContext<LccapDbContext>((serviceProvider, options) =>
        {
            _ = options.UseNpgsql(connectionString).AddInterceptors(
                serviceProvider.GetRequiredService<AuditSaveChangesInterceptor>());
        });
        _ = services.AddScoped<ILccapDbContext>(serviceProvider => serviceProvider.GetRequiredService<LccapDbContext>());
    }
}
