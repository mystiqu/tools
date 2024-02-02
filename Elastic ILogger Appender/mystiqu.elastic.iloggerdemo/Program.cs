using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using mystiqu.elastic.iloggerdemo.extensions;
using mystiqu.elastic.iloggerdemo.logging;

//var config = new ConfigurationBuilder()
//                 .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
//                 //.AddEnvironmentVariables()
//                 .Build();

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

builder.Logging.ClearProviders();
builder.Logging.AddColorConsoleLogger(configuration =>
{
    // Replace warning value from appsettings.json of "Cyan"
    configuration.LogLevelToColorMap[LogLevel.Warning] = ConsoleColor.DarkCyan;
    // Replace warning value from appsettings.json of "Red"
    configuration.LogLevelToColorMap[LogLevel.Error] = ConsoleColor.DarkRed;
});

using IHost host = builder.Build();
var logger = host.Services.GetRequiredService<ILogger<Program>>();

//using var loggerFactory = LoggerFactory.Create(builder =>
//{
//    builder.AddProvider(new ColorConsoleLoggerProvider());
//});
//var logger = loggerFactory.CreateLogger<Program>();

logger.LogDebug(1, "Does this line get hit?");    // Not logged
logger.LogInformation(3, "Nothing to see {test} here.", "hejsan"); // Logs in ConsoleColor.DarkGreen
logger.LogWarning(5, "Warning... that was odd."); // Logs in ConsoleColor.DarkCyan
logger.LogError(7, "Oops, there was an error.");  // Logs in ConsoleColor.DarkRed
logger.LogTrace(5, "== 120.");                    // Not logged

Console.ReadKey();
//await host.RunAsync();