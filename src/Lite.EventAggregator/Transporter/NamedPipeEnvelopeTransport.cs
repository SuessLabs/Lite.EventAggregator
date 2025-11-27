// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Lite.EventAggregator.Transporter;

/// <summary>
///   Provides an async + bi-directional event envelope transport mechanism
///   using named pipes for inter-process communication.
///   Framing: 4-byte length prefix (little-endian), then JSON.
/// </summary>
/// <remarks>
///   - Process A listens on incomingPipeName for requests
///   - Sends responses to replyPipeName advertised by Process B.
///   - Process B does the inverse.
///
///   This class enables sending and receiving event envelopes between processes via named pipes. It
///   supports asynchronous operations for both sending messages and listening for incoming messages or responses.
///   NamedPipeEnvelopeTransport is suitable for scenarios where low-latency, local IPC is required and both communicating
///   parties have access to the same machine. Thread safety is ensured for concurrent send and receive
///   operations.
/// </remarks>
public class NamedPipeEnvelopeTransport : IEventEnvelopeTransport
{
  /// <summary>Framing: 4-byte length prefix (little-endian).</summary>
  private const int LengthPrefixSize = 4;

  /// <summary>Where this process listens for requests/publish.</summary>
  private readonly string _incomingPipeName;

  /// <summary>Where this process sends requests/publish to the other side.</summary>
  private readonly string _outgoingPipeName;

  /// <summary>Where this process listens for responses (advertised as ReplyAddress).</summary>
  private readonly string _replyPipeName;

  public NamedPipeEnvelopeTransport(string incomingPipeName, string outgoingPipeName, string replyPipeName)
  {
    _incomingPipeName = incomingPipeName;
    _outgoingPipeName = outgoingPipeName;
    _replyPipeName = replyPipeName;
  }

  public string ReplyAddress => _replyPipeName;

  public async Task SendAsync(EventEnvelope envelope, CancellationToken cancellationToken = default)
  {
    const string ServerName = ".";
    var targetPipe = envelope.IsResponse && !string.IsNullOrEmpty(envelope.ReplyTo)
      ? envelope.ReplyTo!
      : _outgoingPipeName;

    using var client = new NamedPipeClientStream(ServerName, targetPipe, PipeDirection.Out, PipeOptions.Asynchronous);
    await client.ConnectAsync(cancellationToken).ConfigureAwait(false);
    await WriteEnvelopeAsync(client, envelope, cancellationToken).ConfigureAwait(false);
    await client.FlushAsync(cancellationToken).ConfigureAwait(false);
  }

  public async Task StartAsync(Func<EventEnvelope, Task> onMessageAsync, CancellationToken cancellationToken = default)
  {
    // Start two server loops: requests (incoming) and responses (reply)
    _ = Task.Run(() => ServerLoopAsync(_incomingPipeName, onMessageAsync, cancellationToken), cancellationToken);
    _ = Task.Run(() => ServerLoopAsync(_replyPipeName, onMessageAsync, cancellationToken), cancellationToken);
    await Task.CompletedTask;
  }

  private static async Task<EventEnvelope?> ReadEnvelopeAsync(Stream stream, CancellationToken ct)
  {
    var lenBuf = new byte[LengthPrefixSize];
    var read = await stream.ReadAsync(lenBuf.AsMemory(0, LengthPrefixSize), ct).ConfigureAwait(false);

    if (read == 0)
      return null;

    if (read < LengthPrefixSize)
      return null;

    var length = BitConverter.ToInt32(lenBuf, 0);
    var payload = new byte[length];
    var total = 0;

    while (total < length)
    {
      var r = await stream.ReadAsync(payload.AsMemory(total, length - total), ct).ConfigureAwait(false);
      if (r == 0)
        break;

      total += r;
    }

    if (total < length)
      return null;

    var json = Encoding.UTF8.GetString(payload);
    return JsonSerializer.Deserialize<EventEnvelope>(json);
  }

  /// <summary>Read incoming payloads in a loop and invoke the callback.</summary>
  /// <param name="pipeName">Pipe Name.</param>
  /// <param name="onMessageAsync">Message handler.</param>
  /// <param name="ct"><see cref="CancellationToken"/>.</param>
  /// <returns>Task.</returns>
  private static async Task ServerLoopAsync(string pipeName, Func<EventEnvelope, Task> onMessageAsync, CancellationToken ct)
  {
    while (!ct.IsCancellationRequested)
    {
      using var server = new NamedPipeServerStream(
        pipeName,
        PipeDirection.InOut,
        NamedPipeServerStream.MaxAllowedServerInstances,
        PipeTransmissionMode.Byte,
        PipeOptions.Asynchronous);

      await server.WaitForConnectionAsync(ct).ConfigureAwait(false);

      try
      {
        // Read multiple messages on this connection
        ////while (server.IsConnected && !ct.IsCancellationRequested)
        ////{
        var envelope = await ReadEnvelopeAsync(server, ct).ConfigureAwait(false);
        if (envelope is not null)
          await onMessageAsync(envelope).ConfigureAwait(false);
        ////}
      }
      catch
      {
        /* Swallow for until we add logging */
      }
      finally
      {
        try
        {
          server.Disconnect();
        }
        catch
        {
          /* ignore */
        }
      }
    }
  }

  private static async Task WriteEnvelopeAsync(Stream stream, EventEnvelope envelope, CancellationToken ct)
  {
    var json = JsonSerializer.Serialize(envelope);
    var payload = Encoding.UTF8.GetBytes(json);
    var lengthPrefix = BitConverter.GetBytes(payload.Length);

    await stream.WriteAsync(lengthPrefix.AsMemory(0, 4), ct);
    await stream.WriteAsync(payload.AsMemory(), ct);
  }
}
