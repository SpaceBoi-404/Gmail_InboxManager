using System.Text;
using System.Text.Json;
using Gmail_InboxManager.Application.Abstractions;
using Gmail_InboxManager.Application.Models;
using Gmail_InboxManager.Infrastructure.Configuration;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Microsoft.Extensions.Options;

namespace Gmail_InboxManager.Infrastructure.Gmail;

public sealed class GmailService : IGmailService
{
    private const string UserId = "user";
    private readonly GmailOptions _options;
    private readonly GoogleAuthorizationCodeFlow _flow;

    public GmailService(IOptions<GmailOptions> options)
    {
        _options = options.Value;
        ValidateOptions(_options);

        var credentials = LoadClientSecrets(_options.CredentialsPath);

        _flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = credentials,
            Scopes = new[] { Google.Apis.Gmail.v1.GmailService.Scope.GmailModify },
            DataStore = new FileDataStore(_options.TokenStorePath, fullPath: true)
        });
    }

    public string CreateAuthorizationUrl()
    {
        return _flow.CreateAuthorizationCodeRequest(_options.RedirectUri)
            .Build()
            .ToString();
    }

    public async Task CompleteAuthorizationAsync(
        string code,
        CancellationToken cancellationToken = default)
    {
        await _flow.ExchangeCodeForTokenAsync(
            UserId,
            code,
            _options.RedirectUri,
            cancellationToken);
    }

    public async Task<bool> IsAuthorizedAsync()
    {
        return await _flow.DataStore.GetAsync<TokenResponse>(UserId) is not null;
    }

    public async Task<GmailMessage?> GetLatestMessageAsync(
        CancellationToken cancellationToken = default)
    {
        var token = await _flow.DataStore.GetAsync<TokenResponse>(UserId)
            ?? throw new InvalidOperationException("Użytkownik nie jest autoryzowany.");

        var service = CreateGmailClient(token);
        var listRequest = service.Users.Messages.List("me");
        listRequest.MaxResults = 1;

        var response = await listRequest.ExecuteAsync(cancellationToken);
        var messageId = response.Messages?.FirstOrDefault()?.Id;
        if (string.IsNullOrEmpty(messageId))
        {
            return null;
        }

        var messageRequest = service.Users.Messages.Get("me", messageId);
        messageRequest.Format = UsersResource.MessagesResource.GetRequest.FormatEnum.Full;

        var message = await messageRequest.ExecuteAsync(cancellationToken);
        var subject = GetHeaderValue(message, "Subject") ?? "(Brak tematu)";
        var body = DecodeBody(message);

        return new GmailMessage(messageId, subject, body);
    }

    private Google.Apis.Gmail.v1.GmailService CreateGmailClient(TokenResponse token)
    {
        var credential = new UserCredential(_flow, UserId, token);

        return new Google.Apis.Gmail.v1.GmailService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = _options.ApplicationName
        });
    }

    private static ClientSecrets LoadClientSecrets(string credentialsPath)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(credentialsPath));
        var webCredentials = document.RootElement.GetProperty("web");

        return new ClientSecrets
        {
            ClientId = webCredentials.GetProperty("client_id").GetString()
                ?? throw new InvalidOperationException("Brak client_id w pliku credentials.json."),
            ClientSecret = webCredentials.GetProperty("client_secret").GetString()
                ?? throw new InvalidOperationException("Brak client_secret w pliku credentials.json.")
        };
    }

    private static string? GetHeaderValue(Message message, string headerName)
    {
        return message.Payload?.Headers?
            .FirstOrDefault(header =>
                string.Equals(header.Name, headerName, StringComparison.OrdinalIgnoreCase))
            ?.Value;
    }

    private static string DecodeBody(Message message)
    {
        var encodedBody = message.Payload?.Parts?
            .FirstOrDefault(part => part.MimeType == "text/plain")
            ?.Body?.Data
            ?? message.Payload?.Body?.Data;

        if (string.IsNullOrEmpty(encodedBody))
        {
            return string.Empty;
        }

        var base64 = encodedBody.Replace('-', '+').Replace('_', '/');
        base64 = base64.PadRight(base64.Length + (4 - base64.Length % 4) % 4, '=');

        return Encoding.UTF8.GetString(Convert.FromBase64String(base64));
    }

    private static void ValidateOptions(GmailOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.RedirectUri))
        {
            throw new InvalidOperationException("Konfiguracja Gmail:RedirectUri jest wymagana.");
        }

        if (string.IsNullOrWhiteSpace(options.CredentialsPath))
        {
            throw new InvalidOperationException("Konfiguracja Gmail:CredentialsPath jest wymagana.");
        }
    }
}
