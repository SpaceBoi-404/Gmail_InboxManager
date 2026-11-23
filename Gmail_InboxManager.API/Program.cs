using Gmail_InboxManager.API.Endpoints;
using Gmail_InboxManager.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.MapGmailEndpoints();

app.Run();
