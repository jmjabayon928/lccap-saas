namespace Lccap.Infrastructure.Tests;

public class InfrastructureSmokeTests
{
    [Fact]
    public void InfrastructureAssembly_Loads()
    {
        Assert.NotNull(typeof(Lccap.Infrastructure.InfrastructureAssemblyMarker));
    }
}
