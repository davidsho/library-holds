using System.Text.Json.Serialization;
using LibraryHolds.Api.Copies;
using LibraryHolds.Api.Data;
using LibraryHolds.Api.Holds;
using LibraryHolds.Api.Notifications;
using LibraryHolds.Api.Time;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Settings are read when services are resolved, not here, so tests can replace them.
builder.Services.AddDbContext<LibraryDbContext>((services, options) =>
    options.UseNpgsql(services.GetRequiredService<IConfiguration>().GetConnectionString("Library")));
builder.Services.AddHttpClient<NotificationProvider>((services, client) =>
    client.BaseAddress = new Uri(services.GetRequiredService<IConfiguration>()["NotificationProvider:BaseUrl"]!));
builder.Services.AddOptions<OutboxOptions>().BindConfiguration("Outbox");

builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddScoped<IHoldRepository, HoldRepository>();
builder.Services.AddScoped<HoldMatcher>();
builder.Services.AddScoped<Returns>();
builder.Services.AddHostedService<OutboxDispatcher>();

var app = builder.Build();

// The sample creates its schema on start-up instead of using migrations, to stay short.
using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider.GetRequiredService<LibraryDbContext>().Database.EnsureCreatedAsync();
}

app.MapHoldEndpoints();
app.MapCopyEndpoints();
app.MapOutboxEndpoints();

await app.RunAsync();

public partial class Program;
