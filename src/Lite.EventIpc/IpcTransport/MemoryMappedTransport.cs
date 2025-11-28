// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System;
using System.IO.MemoryMappedFiles;
using System.Runtime.Versioning;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lite.EventAggregator.IpcTransport;

/// <summary>Memory-Mapped IPC Transport (Windows OS only).</summary>
[SupportedOSPlatform("windows")]
public class MemoryMappedTransport : IEventTransport
{
  private const int BufferSize = 4096;
  private readonly string _mapName;
  private CancellationToken _cancelToken;
  private CancellationTokenSource? _cts;

  public MemoryMappedTransport(string mapName)
  {
    _mapName = mapName;
  }

  public void Send<TEvent>(TEvent eventData)
  {
    if (_mapName is null)
      throw new InvalidOperationException("Missing map name");

    // Serialize and write to memory-mapped file
    var json = EventSerializer.Serialize(eventData);
    using var mmf = MemoryMappedFile.CreateOrOpen(_mapName, BufferSize);
    using var accessor = mmf.CreateViewAccessor();

    var bytes = Encoding.UTF8.GetBytes(json);
    accessor.WriteArray(0, bytes, 0, bytes.Length);
  }

  public void StartListening<TEvent>(Action<TEvent> onEventReceived)
  {
    if (_mapName is null)
      throw new InvalidOperationException("Missing map name");

    _cts = new CancellationTokenSource();
    _cancelToken = _cts.Token;

    // Poll memory-mapped file for changes
    Task.Run(() =>
    {
      while (!_cancelToken.IsCancellationRequested)
      {
        // Don't just open, we need to create or open to avoid exceptions
        using var mmf = MemoryMappedFile.CreateOrOpen(_mapName, BufferSize);
        ////using var mmf = MemoryMappedFile.OpenExisting(_mapName);
        using var accessor = mmf.CreateViewAccessor();
        var length = accessor.ReadInt32(0);

        if (length <= 0)
          continue;

        var bytes = new byte[BufferSize];
        accessor.ReadArray(0, bytes, 0, bytes.Length);

        var json = Encoding.UTF8.GetString(bytes).TrimEnd('\0');
        try
        {
          var evt = EventSerializer.Deserialize<TEvent>(json);
          onEventReceived(evt);
        }
        catch (Exception)
        {
          throw new InvalidCastException("Invalid JSON payload");
        }
      } // While not cancelled
    });
  }

  /// <summary>Stop Memory-Mapp Server Listening.</summary>
  public void StopListening()
  {
    _cts?.Cancel();
  }
}
