// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lite.EventAggregator.IpcTransport;

/// <summary>TCP/IP IPC Transport.</summary>
public class TcpTransport : IEventTransport
{
  private readonly string _host;
  private readonly int _port;
  private CancellationToken _cancelToken;
  private CancellationTokenSource? _cts;

  public TcpTransport(string host, int port)
  {
    _host = host;
    _port = port;
  }

  public void Send<TEvent>(TEvent eventData)
  {
    // Serialize and send over TCP socket
    var json = EventSerializer.Serialize(eventData);
    using var client = new TcpClient(_host, _port);

    var stream = client.GetStream();
    var bytes = Encoding.UTF8.GetBytes(json);

    stream.Write(bytes, 0, bytes.Length);
  }

  public void StartListening<TEvent>(Action<TEvent> onEventReceived)
  {
    _cts = new CancellationTokenSource();
    _cancelToken = _cts.Token;

    // Listen on TCP socket and deserialize
    Task.Run(() =>
    {
      var listener = new TcpListener(IPAddress.Any, _port);
      listener.Start();

      try
      {
        while (!_cancelToken.IsCancellationRequested)
        {
          using var client = listener.AcceptTcpClient();
          using var stream = client.GetStream();

          var buffer = new byte[4096];
          var bytesRead = stream.Read(buffer, 0, buffer.Length);
          // if (bytesRead <= 0) continue;

          var json = Encoding.UTF8.GetString(buffer, 0, bytesRead);
          var evt = EventSerializer.Deserialize<TEvent>(json);

          onEventReceived(evt);
        }
      }
      catch (Exception)
      {
        /* Swallow exceptions until we get logging coming later */
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
    });
  }

  /// <inheritdoc/>
  public void StopListening()
  {
    _cts?.Cancel();
  }
}
