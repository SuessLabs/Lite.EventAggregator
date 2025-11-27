// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Lite.EventAggregator.Transporter;

/// <summary>
///   Provides TCP-based transport for sending and receiving async bi-directional event envelopes,
///   supporting both request/reply and publish/subscribe messaging patterns.
/// </summary>
/// <remarks>
///   - Server listens on requestPort for requests/publishes.
///   - Responses are sent to replyPort where the requester listens.
///   - Message framing: 4-byte length prefix + JSON.
///
///   TcpEnvelopeTransport enables communication over TCP sockets by listening for incoming event envelopes
///   and sending outgoing envelopes to specified endpoints. It supports asynchronous message handling and can be
///   integrated into distributed event-driven systems. This class is not thread-safe; concurrent usage should be managed
///   externally if required.
/// </remarks>
public class TcpEnvelopeTransport : IEventEnvelopeTransport
{
  private readonly IPEndPoint _replyListen;

  /// <summary>Where to send responses.</summary>
  private readonly IPEndPoint _replySend;

  private readonly IPEndPoint _requestListen;

  /// <summary>Where to send requests/publishes.</summary>
  private readonly IPEndPoint _requestSend;

  public TcpEnvelopeTransport(IPEndPoint requestListen, IPEndPoint replyListen, IPEndPoint requestSend, IPEndPoint replySend)
  {
    _requestListen = requestListen;
    _replyListen = replyListen;
    _requestSend = requestSend;
    _replySend = replySend;
  }

  public string ReplyAddress => $"{_replyListen.Address}:{_replyListen.Port}";

  public async Task SendAsync(EventEnvelope envelope, CancellationToken cancellationToken = default)
  {
    var target = envelope.IsResponse
      ? _replySend
      : _requestSend;

    using var client = new TcpClient();

    await client.ConnectAsync(target.Address, target.Port, cancellationToken).ConfigureAwait(false);
    using var stream = client.GetStream();

    await WriteEnvelopeAsync(stream, envelope, cancellationToken).ConfigureAwait(false);
    await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
  }

  public async Task StartAsync(Func<EventEnvelope, Task> onMessageAsync, CancellationToken cancellationToken = default)
  {
    _ = Task.Run(() => ListenLoopAsync(_requestListen, onMessageAsync, cancellationToken), cancellationToken);
    _ = Task.Run(() => ListenLoopAsync(_replyListen, onMessageAsync, cancellationToken), cancellationToken);
    await Task.CompletedTask;
  }

  private static async Task ListenLoopAsync(IPEndPoint endpoint, Func<EventEnvelope, Task> onMessageAsync, CancellationToken ct)
  {
    var listener = new TcpListener(endpoint);
    listener.Start();
    try
    {
      while (!ct.IsCancellationRequested)
      {
        var client = await listener.AcceptTcpClientAsync(ct).ConfigureAwait(false);

        _ = Task.Run(async () =>
        {
          using var c = client;
          using var s = c.GetStream();
          while (!ct.IsCancellationRequested)
          {
            var envelope = await ReadEnvelopeAsync(s, ct).ConfigureAwait(false);
            if (envelope == null)
              break;

            await onMessageAsync(envelope).ConfigureAwait(false);
          }
        },
        ct);
      }
    }
    catch
    {
      /* swallow for demo */
    }
    finally
    {
      try
      {
        listener.Stop();
      }
      catch
      {
      }
    }
  }

  private static async Task<EventEnvelope?> ReadEnvelopeAsync(NetworkStream stream, CancellationToken ct)
  {
    var lenBuf = new byte[4];
    var read = await stream.ReadAsync(lenBuf.AsMemory(0, 4), ct).ConfigureAwait(false);

    if (read == 0)
      return null;

    if (read < 4)
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

  private static async Task WriteEnvelopeAsync(NetworkStream stream, EventEnvelope envelope, CancellationToken ct)
  {
    var json = JsonSerializer.Serialize(envelope);
    var bytes = Encoding.UTF8.GetBytes(json);
    var len = BitConverter.GetBytes(bytes.Length);

    await stream.WriteAsync(len.AsMemory(0, 4), ct);
    await stream.WriteAsync(bytes.AsMemory(), ct);
  }
}
