namespace MessageReciever.Models;

public record RawMessage(byte[] DeviceId, ushort MessageCounter, byte MessageType, byte[] Payload);
