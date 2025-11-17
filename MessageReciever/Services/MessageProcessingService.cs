using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading.Channels;
using MessageReciever.Models;

namespace MessageReciever.Services;

public sealed class MessageProcessingService : BackgroundService
{
	private readonly Channel<RawMessage> _rawChannel;
	private readonly Channel<string> _deviceMessageChannel;
	private readonly Channel<DeviceEvent> _eventChannel;
	private readonly ConcurrentDictionary<string, ushort> _lastSeenCounters = new();
	private readonly ILogger<MessageProcessingService> _logger;

	public MessageProcessingService(Channel<RawMessage> rawChannel,
							   Channel<string> deviceMessageChannel,
							   Channel<DeviceEvent> eventChannel,
							   ILogger<MessageProcessingService> logger)
	{
		_rawChannel = rawChannel;
		_deviceMessageChannel = deviceMessageChannel;
		_eventChannel = eventChannel;
		_logger = logger;
	}

	protected override async Task ExecuteAsync(CancellationToken ct)
	{
		await foreach (var msg in _rawChannel.Reader.ReadAllAsync(ct))
		{
			// Lock-free deduplication
			if (!IsNewMessage(msg.DeviceId, msg.MessageCounter))
			{
				_logger.LogInformation("Duplicate/older message ignored - Device: {DeviceId}, Counter: {Counter}", Convert.ToHexString(msg.DeviceId), msg.MessageCounter);
				continue;
			}

			switch (msg.MessageType)
			{
				case 2 or 11 or 13: // Device Messages
					var json = JsonSerializer.Serialize(new
					{
						DeviceId = Convert.ToHexString(msg.DeviceId),
						msg.MessageCounter,
						msg.MessageType,
						ReceivedAt = DateTime.UtcNow,
						PayloadBase64 = Convert.ToBase64String(msg.Payload)
					});

					await _deviceMessageChannel.Writer.WriteAsync(json, ct);
					_logger.LogInformation("Routed Device Message → Queue (DeviceId={DeviceId}, Counter={Counter}, Type={Type})", Convert.ToHexString(msg.DeviceId), msg.MessageCounter, msg.MessageType);
					break;

				case 1 or 3 or 12 or 14: // Device Events
					await _eventChannel.Writer.WriteAsync(new DeviceEvent(msg.DeviceId, msg.MessageCounter, msg.MessageType, msg.Payload), ct);
					_logger.LogInformation("Routed Device Event → Queue (DeviceId={DeviceId}, Counter={Counter}, Type={Type}, PayloadLen={Len})", Convert.ToHexString(msg.DeviceId), msg.MessageCounter, msg.MessageType, msg.Payload.Length);
					break;

				default:
					_logger.LogWarning("Unknown MessageType {Type} from Device {DeviceId} - discarded", msg.MessageType, Convert.ToHexString(msg.DeviceId));
					break;
			}
		}
	}

	private bool IsNewMessage(byte[] deviceId, ushort counter)
	{
		string deviceKey = Convert.ToHexString(deviceId);

		while (true)
		{
			if (_lastSeenCounters.TryGetValue(deviceKey, out ushort seen))
			{
				if (counter <= seen) return false;

				if (_lastSeenCounters.TryUpdate(deviceKey, counter, seen))
					return true;
			}
			else
			{
				if (_lastSeenCounters.TryAdd(deviceKey, counter))
					return true;
			}
		}
	}
}