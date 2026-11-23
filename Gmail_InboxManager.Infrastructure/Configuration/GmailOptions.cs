namespace Gmail_InboxManager.Infrastructure.Configuration;

public sealed class GmailOptions
{
    public const string SectionName = "Gmail";

    public string CredentialsPath { get; set; } = "credentials.json";

    public string RedirectUri { get; set; } = string.Empty;

    public string ApplicationName { get; set; } = "Gmail Inbox Manager";

    public string TokenStorePath { get; set; } = Path.GetTempPath();
}
