using Lccap.Application.Common.Interfaces;
using Lccap.Infrastructure.Persistence;
using Lccap.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Lccap.Infrastructure.Tests.Persistence;

public class LccapDbContextSmokeTests
{
    [Fact]
    public void DbContext_WithNpgsqlOptions_Builds_Model_WithoutConnecting()
    {
        var builder = new DbContextOptionsBuilder<LccapDbContext>().UseNpgsql(
            // Valid shape; EF does not establish a TCP connection until a query/database API is invoked.
            "Host=127.0.0.1;Port=65432;Database=lccap_smoke;Username=test;Password=test");

        using var ctx = new LccapDbContext(builder.Options);

        _ = ctx.Model;

        Assert.NotNull(ctx.Model);
    }

    [Fact]
    public void AddApplicationServices_registers_singleton_clock_and_scoped_CurrentUser_context()
    {
        var services = new ServiceCollection();
        Lccap.Application.DependencyInjection.AddApplicationServices(services);
        using var provider = services.BuildServiceProvider();

        var clock = provider.GetRequiredService<IClock>();
        Assert.Same(clock, provider.GetRequiredService<IClock>());

        using var scope = provider.CreateScope();
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ICurrentUserContext>());
    }

    [Fact]
    public void AuditSaveChangesInterceptor_can_be_constructed_via_DI()
    {
        var services = new ServiceCollection();
        Lccap.Application.DependencyInjection.AddApplicationServices(services);
        services.AddScoped<AuditSaveChangesInterceptor>();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetRequiredService<AuditSaveChangesInterceptor>());
    }
}
