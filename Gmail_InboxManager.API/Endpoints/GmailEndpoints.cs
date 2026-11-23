using Gmail_InboxManager.Application.Abstractions;

namespace Gmail_InboxManager.API.Endpoints;

public static class GmailEndpoints
{
    public static IEndpointRouteBuilder MapGmailEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/login", (IGmailService gmailService) =>
            Results.Redirect(gmailService.CreateAuthorizationUrl()));

        endpoints.MapGet("/callback", async (
            string? code,
            IGmailService gmailService,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return Results.BadRequest("Brak kodu autoryzacyjnego.");
            }

            await gmailService.CompleteAuthorizationAsync(code, cancellationToken);
            return Results.Ok(
                "Zalogowano pomyślnie! Zamknij tę kartę i wejdź na /api/test-gmail");
        });

        endpoints.MapGet("/api/test-gmail", async (
            IGmailService gmailService,
            CancellationToken cancellationToken) =>
        {
            if (!await gmailService.IsAuthorizedAsync())
            {
                return Results.Redirect("/login");
            }

            var message = await gmailService.GetLatestMessageAsync(cancellationToken);
            if (message is null)
            {
                return Results.Ok("Brak wiadomości.");
            }

            return Results.Ok(new
            {
                status = "Sukces!",
                id = message.Id,
                temat = message.Subject,
                tresc = message.Body
            });
        });

        return endpoints;
    }
}
