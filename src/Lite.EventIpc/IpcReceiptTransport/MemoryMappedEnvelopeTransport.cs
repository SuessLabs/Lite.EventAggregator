// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.
#if PREVIEW

using System;
using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using System.Runtime.Versioning;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Lite.EventIpc.IpcReceiptTransport;

/// <summary>Memory-Mapped IPC Transport - Async + Bi-Directional (Windows OS only).</summary>
/// <remarks>
///   - Two MMFs: requestMapName, responseMapName
///   - Two EventWaitHandles: requestSignalName, responseSignalName
///   - Writer writes JSON, then signals.Reader waits, reads, and dispatches.
///   - ! Simple beta: Single message buffer (overwrite), no multi-producer queueing.
/// </remarks>
[SupportedOSPlatform("windows")]
public class MemoryMappedEnvelopeTransport : IEventEnvelopeTransport
{
  /// <summary>1MB buffer for beta.</summary>
  private const int DefaultBufferSize = 1 << 20;

  private const int LengthPrefixSize = 4;

  private readonly int _bufferSize;
  private readonly string _requestMapName;
  private readonly string _requestSignalName;
  private readonly string _responseMapName;
  private readonly string _responseSignalName;

  public MemoryMappedEnvelopeTransport(
    string requestMapName,
    string responseMapName,
    string requestSignalName,
    string responseSignalName,
    int bufferSize = DefaultBufferSize)
  {
    _requestMapName = requestMapName;
    _responseMapName = responseMapName;
    _requestSignalName = requestSignalName;
    _responseSignalName = responseSignalName;
    _bufferSize = bufferSize;
  }

  /// <summary>Gets semantic only; actual routing uses known map/signal.</summary>
  public string ReplyAddress => _responseMapName;

  public async Task SendAsync(EventEnvelope envelope, CancellationToken cancellationToken = default)
  {
    var mapName = envelope.IsResponse ? _responseMapName : _requestMapName;
    var signalName = envelope.IsResponse ? _responseSignalName : _requestSignalName;

    using var mmf = MemoryMappedFile.CreateOrOpen(mapName, DefaultBufferSize);
    using var accessor = mmf.CreateViewAccessor();

    var json = JsonSerializer.Serialize(envelope);
    var bytes = Encoding.UTF8.GetBytes(json);

    if (bytes.Length + LengthPrefixSize > DefaultBufferSize)
      throw new InvalidOperationException("Payload too large for MMF buffer.");

    accessor.Write(0, bytes.Length);
    accessor.WriteArray(LengthPrefixSize, bytes, 0, bytes.Length);

    using var ev = new EventWaitHandle(false, EventResetMode.AutoReset, signalName);
    ev.Set();
    await Task.CompletedTask;
  }

  public async Task StartAsync(Func<EventEnvelope, Task> onMessageAsync, CancellationToken cancellationToken = default)
  {
    // Start listeners for request and response channels
    _ = Task.Run(() => MapListenerAsync(_requestMapName, _requestSignalName, onMessageAsync, _bufferSize, cancellationToken), cancellationToken);
    _ = Task.Run(() => MapListenerAsync(_responseMapName, _responseSignalName, onMessageAsync, _bufferSize, cancellationToken), cancellationToken);
    await Task.CompletedTask;
  }

  private static async Task MapListenerAsync(
    string mapName,
    string signalName,
    Func<EventEnvelope, Task> onMessageAsync,
    int bufferSize,
    CancellationToken ct)
  {
    using var ev = new EventWaitHandle(false, EventResetMode.AutoReset, signalName);
    while (!ct.IsCancellationRequested)
    {
      // Wait for signal
      ev.WaitOne();

      try
      {
        using var mmf = MemoryMappedFile.CreateOrOpen(mapName, bufferSize);
        using var accessor = mmf.CreateViewAccessor();
        var length = accessor.ReadInt32(0);

        if (length <= 0 || length > DefaultBufferSize - LengthPrefixSize)
          continue;

        var buffer = new byte[length];
        accessor.ReadArray(LengthPrefixSize, buffer, 0, length);

        var json = Encoding.UTF8.GetString(buffer);
        var envelope = JsonSerializer.Deserialize<EventEnvelope>(json);

        if (envelope != null)
          await onMessageAsync(envelope).ConfigureAwait(false);
      }
      catch
      {
        /* swallow until we get logging */
        Debug.WriteLine("Exception: MemoryMap (Receipted) Lister");
      }
    }
  }
}

#endif
