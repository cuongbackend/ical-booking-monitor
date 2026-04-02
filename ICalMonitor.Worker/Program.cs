using ICalMonitor.Worker;
using ICalMonitor.Worker.Models;
using ICalMonitor.Worker.Services;

var builder = Host.CreateApplicationBuilder(args);

// Config
builder.Services.Configure<AppConfig>(builder.Configuration.GetSection("Monitor"));

// HTTP client cho iCal (timeout 15s, User-Agent)
builder.Services.AddHttpClient("ical", client =>
{
    client.Timeout = TimeSpan.FromSeconds(15);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("ICalMonitor/1.0");
});

// Services
builder.Services.AddSingleton<StateService>();
builder.Services.AddScoped<ICalService>();
builder.Services.AddScoped<TelegramService>();

// Worker
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
