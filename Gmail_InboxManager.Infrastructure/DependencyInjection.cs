using Gmail_InboxManager.Application.Abstractions;
using Gmail_InboxManager.Infrastructure.Configuration;
using Gmail_InboxManager.Infrastructure.Gmail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Gmail_InboxManager.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<GmailOptions>(
            configuration.GetSection(GmailOptions.SectionName));
        services.AddSingleton<IGmailService, GmailService>();

        return services;
    }
}
