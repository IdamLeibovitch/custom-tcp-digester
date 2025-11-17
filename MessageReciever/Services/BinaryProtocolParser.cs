// using Microsoft.Extensions.Logging;
using System.Buffers;
using System.Buffers.Binary;
using MessageReciever.Models;
using Microsoft.Extensions.Logging;

namespace MessageReciever.Services;

public static class BinaryProtocolParser
{
    public static bool TryParse(ref SequenceReader<byte> reader, out RawMessage message, ILogger logger)
    {
        message = default!;

        // Resynchronize to next 0xAA55 sync word (little-endian from BinaryWriter)
        while (reader.Remaining >= 2 && !(reader.UnreadSpan[0] == 0x55 && reader.UnreadSpan[1] == 0xAA))
        {
            reader.Advance(1);
        }

        if (reader.Remaining < 11) // need at least header after sync
            return false;

        reader.Advance(2); // consume sync word 0x55AA (little-endian)

        // Read fixed 9-byte header safely and use BinaryReader
        Span<byte> header = stackalloc byte[9];
        reader.UnreadSequence.Slice(0, 9).CopyTo(header);
        reader.Advance(9);

        using var headerStream = new MemoryStream(header.ToArray());
        using var binaryReader = new BinaryReader(headerStream);

        byte[] deviceId = binaryReader.ReadBytes(4);
        ushort counter = BinaryPrimitives.ReadUInt16LittleEndian(binaryReader.ReadBytes(2));
        byte messageType = binaryReader.ReadByte();
        ushort payloadLength = BinaryPrimitives.ReadUInt16LittleEndian(binaryReader.ReadBytes(2));

        if (reader.Remaining < payloadLength)
        {
            reader.Rewind(11); // put back sync + header because payload incomplete
            return false;
        }

        var payloadSeq = reader.UnreadSequence.Slice(0, payloadLength);
        byte[] payload = payloadSeq.ToArray();
        reader.Advance(payloadLength);

        message = new RawMessage(deviceId, counter, messageType, payload);
        return true;
    }
}