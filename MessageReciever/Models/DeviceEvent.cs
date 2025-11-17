namespace MessageReciever.Models;

public record DeviceEvent(byte[] DeviceId, ushort MessageCounter, byte MessageType, byte[] Payload);
