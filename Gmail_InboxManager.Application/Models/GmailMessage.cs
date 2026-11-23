namespace Gmail_InboxManager.Application.Models;

public sealed record GmailMessage(
    string Id,
    string Subject,
    string Body);
