using Gmail_InboxManager.Application.Models;

namespace Gmail_InboxManager.Application.Abstractions;

public interface IGmailService
{
    string CreateAuthorizationUrl();

    Task CompleteAuthorizationAsync(string code, CancellationToken cancellationToken = default);

    Task<bool> IsAuthorizedAsync();

    Task<GmailMessage?> GetLatestMessageAsync(CancellationToken cancellationToken = default);
}
