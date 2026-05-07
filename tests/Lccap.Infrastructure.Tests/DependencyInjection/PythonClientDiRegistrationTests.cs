using System;
using System.Collections.Generic;
using ApplicationDi = Lccap.Application.DependencyInjection;
using Lccap.Application.ExposureAnalysisJobs.Computation;
using Lccap.Application.ExposureAnalysisJobs.Computation.Python;
using InfrastructureDi = Lccap.Infrastructure.DependencyInjection;
using Lccap.Infrastructure.ExposureAnalysisJobs.Python;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Lccap.Infrastructure.Tests.DependencyInjection;

public sealed class PythonClientDiRegistrationTests
{
    private static ServiceProvider BuildProvider(IReadOnlyDictionary<string, string?> inMemorySettings)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        var services = new ServiceCollection();
        ApplicationDi.AddApplicationServices(services);
        InfrastructureDi.AddInfrastructureServices(services, configuration);

        return services.BuildServiceProvider();
    }

    [Fact]
    public void AddInfrastructureServices_registers_python_exposure_service_client()
    {
        var provider = BuildProvider(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Postgres"] = "Host=localhost;Port=5432;Database=lccap_test;Username=postgres;Password=postgres",
            ["PythonAi:BaseUrl"] = "http://localhost:8000",
            ["PythonAi:TimeoutSeconds"] = "7",
        });

        var client = provider.GetRequiredService<IPythonExposureComputationServiceClient>();
        Assert.NotNull(client);
    }

    [Fact]
    public void AddInfrastructureServices_registers_python_exposure_adapter()
    {
        var provider = BuildProvider(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Postgres"] = "Host=localhost;Port=5432;Database=lccap_test;Username=postgres;Password=postgres",
            ["PythonAi:BaseUrl"] = "http://localhost:8000",
            ["PythonAi:TimeoutSeconds"] = "7",
        });

        var adapter = provider.GetRequiredService<IPythonExposureComputationClientAdapter>();
        Assert.NotNull(adapter);
    }

    [Fact]
    public void AddInfrastructureServices_preserves_not_configured_computation_client()
    {
        var provider = BuildProvider(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Postgres"] = "Host=localhost;Port=5432;Database=lccap_test;Username=postgres;Password=postgres",
            ["PythonAi:BaseUrl"] = "http://localhost:8000",
            ["PythonAi:TimeoutSeconds"] = "7",
        });

        var computationClient = provider.GetRequiredService<IExposureComputationClient>();
        Assert.IsType<NotConfiguredExposureComputationClient>(computationClient);
    }

    [Fact]
    public void AddInfrastructureServices_binds_python_options()
    {
        var provider = BuildProvider(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Postgres"] = "Host=localhost;Port=5432;Database=lccap_test;Username=postgres;Password=postgres",
            ["PythonAi:BaseUrl"] = "http://localhost:8000",
            ["PythonAi:TimeoutSeconds"] = "7",
            ["PythonAi:ComputePath"] = "/compute/exposure"
        });

        var options = provider.GetRequiredService<IOptions<PythonExposureComputationOptions>>().Value;

        Assert.Equal("http://localhost:8000", options.BaseUrl);
        Assert.Equal(7, options.TimeoutSeconds);
        Assert.Equal("/compute/exposure", options.ComputePath);
    }

    [Fact]
    public void AddInfrastructureServices_binds_python_feature_options_defaults_enabled_false_when_missing()
    {
        var provider = BuildProvider(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Postgres"] = "Host=localhost;Port=5432;Database=lccap_test;Username=postgres;Password=postgres",
            ["PythonAi:BaseUrl"] = "http://localhost:8000",
            ["PythonAi:TimeoutSeconds"] = "7",
        });

        var options = provider.GetRequiredService<IOptions<PythonExposureComputationFeatureOptions>>().Value;
        Assert.False(options.Enabled);
    }

    [Fact]
    public void AddInfrastructureServices_binds_python_feature_options_enabled_when_set()
    {
        var provider = BuildProvider(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Postgres"] = "Host=localhost;Port=5432;Database=lccap_test;Username=postgres;Password=postgres",
            ["PythonAi:BaseUrl"] = "http://localhost:8000",
            ["PythonAi:Enabled"] = "true",
        });

        var options = provider.GetRequiredService<IOptions<PythonExposureComputationFeatureOptions>>().Value;
        Assert.True(options.Enabled);
    }

    [Fact]
    public void AddInfrastructureServices_allows_missing_python_base_url()
    {
        var provider = BuildProvider(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Postgres"] = "Host=localhost;Port=5432;Database=lccap_test;Username=postgres;Password=postgres",
            ["PythonAi:TimeoutSeconds"] = "7",
            ["PythonAi:ComputePath"] = "/compute/exposure",
        });

        var client = provider.GetRequiredService<IPythonExposureComputationServiceClient>();
        Assert.NotNull(client);
    }

    [Fact]
    public void AddInfrastructureServices_allows_invalid_python_base_url()
    {
        var provider = BuildProvider(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Postgres"] = "Host=localhost;Port=5432;Database=lccap_test;Username=postgres;Password=postgres",
            ["PythonAi:BaseUrl"] = "not a valid uri",
            ["PythonAi:TimeoutSeconds"] = "7",
        });

        var client = provider.GetRequiredService<IPythonExposureComputationServiceClient>();
        Assert.NotNull(client);
    }
}

