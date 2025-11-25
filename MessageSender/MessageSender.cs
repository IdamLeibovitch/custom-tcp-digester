using System.Net.Sockets;

class MessageSender
{

    // change this to be using uri instead of ip address and port
    public static void SendMessage(Uri address, byte[] deviceId, short messageCounter, byte messageType, byte[] payload)

    {
        ushort payloadLength = (ushort)payload.Length;

        using var client = new TcpClient(address.Host, address.Port);
        using var stream = client.GetStream();
        using var writer = new BinaryWriter(stream);

        writer.Write(BitConverter.GetBytes((ushort)0xAA55).Reverse().ToArray());
        writer.Write(deviceId.Reverse().ToArray());
        writer.Write(BitConverter.GetBytes(messageCounter).Reverse().ToArray());
        writer.Write(messageType);
        writer.Write(BitConverter.GetBytes(payloadLength).Reverse().ToArray());
        writer.Write(payload);
    }

    public static void SendStream(string address)
    {
        if (!Uri.TryCreate(address, UriKind.Absolute, out var uri))
        {
            throw new ArgumentException("Invalid address format. Use 'hostname:port'.", nameof(address));
        }

        SendStream(uri);
    }
    public static void SendStream(Uri address)
    {
        byte[] firstDeviceId = [0x01, 0x02, 0x03, 0x04];
        byte[] secondDeviceId = [0x05, 0x06, 0x07, 0x08];

        SendMessage(address, firstDeviceId, 1, 0x01, [0x01, 0x02, 0x03]);
        SendMessage(address, firstDeviceId, 1, 0x01, [0x01, 0x02, 0x03]);
        SendMessage(address, firstDeviceId, 1, 0x02, [0x07, 0x08, 0x09]);
        SendMessage(address, secondDeviceId, 1, 0x01, [0x04, 0x05, 0x06]);
    }

}