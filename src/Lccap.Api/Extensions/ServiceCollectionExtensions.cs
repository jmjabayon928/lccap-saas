using ApplicationDi = Lccap.Application.DependencyInjection;
using InfrastructureDi = Lccap.Infrastructure.DependencyInjection;

namespace Lccap.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static WebApplicationBuilder AddLccapServices(this WebApplicationBuilder builder)
    {
        ApplicationDi.AddApplicationServices(builder.Services);
        InfrastructureDi.AddInfrastructureServices(builder.Services, builder.Configuration);

        return builder;
    }
}
