using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Buffers;
using System.Buffers.Binary;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Threading.Channels;
using MessageReciever.Models;

namespace MessageReciever.Services;

public sealed class TcpListenerService : BackgroundService
{
    private readonly Channel<RawMessage> _messageChannel;
    private readonly ILogger<TcpListenerService> _logger;

    public TcpListenerService(Channel<RawMessage> messageChannel, ILogger<TcpListenerService> logger)
    {
        _messageChannel = messageChannel;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var listener = new TcpListener(System.Net.IPAddress.Any, 5011); // Mac and its bulshit
        listener.Start();

        _logger.LogInformation("TCP ingestion microservice listening on port 5011");

        while (!stoppingToken.IsCancellationRequested)
        {
            TcpClient client = await listener.AcceptTcpClientAsync(stoppingToken);
            client.NoDelay = true;

            // Fire-and-forget per connection (each connection gets its own task)
            _ = ProcessConnectionAsync(client, stoppingToken);
        }
    }

    private async Task ProcessConnectionAsync(TcpClient client, CancellationToken ct)
    {
        var pipeReader = PipeReader.Create(client.GetStream(), new StreamPipeReaderOptions(leaveOpen: true));

        try
        {
            while (!ct.IsCancellationRequested)
            {
                ReadResult result = await pipeReader.ReadAsync(ct);

                var buffer = result.Buffer;

                //  SequencePosition consumed = buffer.Start;
                var reader = new SequenceReader<byte>(buffer);

                while (BinaryProtocolParser.TryParse(ref reader, out var message, _logger))
                {
                    _messageChannel.Writer.TryWrite(message);
                    //  await _messageChannel.Writer.WriteAsync(message, ct); 
                    //  consumed = reader.Position;
                }

                pipeReader.AdvanceTo(reader.Position, buffer.End);

                if (result.IsCompleted) break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Connection terminated or error");
        }
        finally
        {
            pipeReader.Complete();
            client.Dispose();
        }
    }
}