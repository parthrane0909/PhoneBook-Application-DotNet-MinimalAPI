namespace Phonebook.Endpoints;

/// <summary>Root endpoint — same body as the old HealthController.</summary>
public static class RootEndpoints
{
    public static void MapRootEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/", Get);
    }

    private static IResult Get() =>
        Results.Ok(new { message = "Phonebook API is running" });
}
