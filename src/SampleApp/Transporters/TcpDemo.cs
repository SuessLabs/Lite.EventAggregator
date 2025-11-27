// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System;
using System.Net;
using System.Threading.Tasks;
using Lite.EventAggregator;
using Lite.EventAggregator.Transporter;

namespace SampleApp.Transporters;

public class TcpDemo
{
  public async Task RunDemoAsync()
  {
    Console.WriteLine("TCP Demo");
    var clientAgg = new EventAggregator();
    var serverAgg = new EventAggregator();

    var serverTransport = new TcpEnvelopeTransport(
        requestListen: new IPEndPoint(IPAddress.Loopback, 7001),
        replyListen: new IPEndPoint(IPAddress.Loopback, 7002),
        requestSend: new IPEndPoint(IPAddress.Loopback, 7003),
        replySend: new IPEndPoint(IPAddress.Loopback, 7004));

    var clientTransport = new TcpEnvelopeTransport(
        requestListen: new IPEndPoint(IPAddress.Loopback, 7003),
        replyListen: new IPEndPoint(IPAddress.Loopback, 7004),
        requestSend: new IPEndPoint(IPAddress.Loopback, 7001),
        replySend: new IPEndPoint(IPAddress.Loopback, 7002));

    await serverAgg.UseIpcEnvelopeTransportAsync(serverTransport);
    await clientAgg.UseIpcEnvelopeTransportAsync(clientTransport);

    serverAgg.SubscribeRequest<Ping, Pong>(req => Task.FromResult(new Pong(req.Message + " handled by tcp")));

    await clientAgg.PublishAsync(new Ping("publish via tcp"));
    var resp = await clientAgg.RequestAsync<Ping, Pong>(new Ping("request via tcp"));
    Console.WriteLine($"[Client] Response: {resp.Message}");
  }

  public record Ping(string Message);

  public record Pong(string Message);
}
