using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MessageReciever.Services;

public sealed class DeviceMessageConsumer : BackgroundService
{
    private readonly Channel<string> _channel;
    private readonly ILogger<DeviceMessageConsumer> _logger;

    public DeviceMessageConsumer(Channel<string> channel, ILogger<DeviceMessageConsumer> logger)
    {
        _channel = channel;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await foreach (var json in _channel.Reader.ReadAllAsync(ct))
        {
            _logger.LogInformation("[Device Message Queue] Processed: {Json}", json);
        }
    }
}