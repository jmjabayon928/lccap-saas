using Microsoft.AspNetCore.Http;

namespace Lccap.Api.Tests;

public class ApiSmokeTests
{
    [Fact]
    public void HealthEndpoint_ReturnsSuccess()
    {
        var result = Lccap.Api.Health.HealthEndpoints.GetHealth();
        var statusResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);

        Assert.True(statusResult.StatusCode is >= 200 and < 300);
    }
}
