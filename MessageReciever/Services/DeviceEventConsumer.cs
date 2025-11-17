using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MessageReciever.Models;

namespace MessageReciever.Services;

public sealed class DeviceEventConsumer : BackgroundService
{
    private readonly Channel<DeviceEvent> _channel;
    private readonly ILogger<DeviceEventConsumer> _logger;

    public DeviceEventConsumer(Channel<DeviceEvent> channel, ILogger<DeviceEventConsumer> logger)
    {
        _channel = channel;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await foreach (var evt in _channel.Reader.ReadAllAsync(ct))
        {
            _logger.LogInformation("[Device Event Queue] Processed event Type={Type} DeviceId={DeviceId} Counter={Counter} PayloadLength={PayloadLength}",
                evt.MessageType, Convert.ToHexString(evt.DeviceId), evt.MessageCounter, evt.Payload.Length);
        }
    }
}