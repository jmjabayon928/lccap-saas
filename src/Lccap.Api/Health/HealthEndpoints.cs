namespace Lccap.Api.Health;

public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/health", GetHealth);
        return endpoints;
    }

    public static IResult GetHealth()
    {
        return Results.Ok(new { status = "healthy" });
    }
}
