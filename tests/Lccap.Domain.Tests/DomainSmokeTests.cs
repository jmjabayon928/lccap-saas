namespace Lccap.Domain.Tests;

public class DomainSmokeTests
{
    [Fact]
    public void DomainAssembly_Loads()
    {
        Assert.NotNull(typeof(Lccap.Domain.DomainAssemblyMarker));
    }
}
