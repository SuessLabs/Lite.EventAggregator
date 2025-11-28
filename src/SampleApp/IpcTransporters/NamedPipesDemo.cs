// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.
#if PREVIEW

using System;
using System.Threading.Tasks;
using Lite.EventAggregator;
using Lite.EventAggregator.IpcReceiptTransport;

namespace SampleApp.IpcTransporters;

public class NamedPipesDemo
{
  public async Task RunDemoAsync()
  {
    Console.WriteLine("Named Pipes Demo");
    var clientAgg = new EventAggregator();
    var serverAgg = new EventAggregator();

    var serverTransport = new NamedPipeEnvelopeTransport("server-requests-in", "client-requests-in", "server-replies-in");
    var clientTransport = new NamedPipeEnvelopeTransport("client-requests-in", "server-requests-in", "client-replies-in");

    await serverAgg.UseIpcEnvelopeTransportAsync(serverTransport);
    await clientAgg.UseIpcEnvelopeTransportAsync(clientTransport);

    serverAgg.Subscribe<Ping>(p => Console.WriteLine($"[Server] Publish: {p.Message}"));
    serverAgg.SubscribeRequest<Ping, Pong>(req => Task.FromResult(new Pong(req.Message + " handled by server")));

    await clientAgg.PublishAsync(new Ping("publish via pipes"));
    var resp = await clientAgg.RequestAsync<Ping, Pong>(new Ping("request via pipes"));
    Console.WriteLine($"[Client] Response: {resp.Message}");
  }

  public record Ping(string Message);

  public record Pong(string Message);
}

#endif
