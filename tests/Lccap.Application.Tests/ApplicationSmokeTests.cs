namespace Lccap.Application.Tests;

public class ApplicationSmokeTests
{
    [Fact]
    public void ApplicationAssembly_Loads()
    {
        Assert.NotNull(typeof(Lccap.Application.ApplicationAssemblyMarker));
    }
}
