using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading.Channels;
using MessageReciever.Models;
using MessageReciever.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options =>
{
    options.IncludeScopes = true;
    options.TimestampFormat = "HH:mm:ss.fff ";
});

builder.Services.AddHostedService<TcpListenerService>();
builder.Services.AddHostedService<MessageProcessingService>();
builder.Services.AddHostedService<DeviceMessageConsumer>();
builder.Services.AddHostedService<DeviceEventConsumer>();

// // Channels
builder.Services.AddSingleton(Channel.CreateUnbounded<RawMessage>());
builder.Services.AddSingleton(Channel.CreateUnbounded<string>()); // Device message JSON queue
builder.Services.AddSingleton(Channel.CreateUnbounded<DeviceEvent>()); // Device event queue

var host = builder.Build();

await host.RunAsync();