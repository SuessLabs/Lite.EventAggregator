// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.
#if PREVIEW

using System;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Lite.EventAggregator;
using Lite.EventAggregator.Transporter;

namespace SampleApp.Transporters;

[SupportedOSPlatform("windows")]
public class MemoryMapDemo
{
  public async Task RunDemoAsync()
  {
    Console.WriteLine("Memory-Mapped Demo");
    var clientAgg = new EventAggregator();
    var serverAgg = new EventAggregator();

    var transport = new MemoryMappedEnvelopeTransport("req-map", "resp-map", "req-signal", "resp-signal");

    // Same transport names used by both sides for demo simplicity
    await serverAgg.UseIpcEnvelopeTransportAsync(transport);
    await clientAgg.UseIpcEnvelopeTransportAsync(transport);

    serverAgg.SubscribeRequest<Ping, Pong>(req => Task.FromResult(new Pong(req.Message + " handled by mmf")));

    await clientAgg.PublishAsync(new Ping("publish via mmf"));
    var resp = await clientAgg.RequestAsync<Ping, Pong>(new Ping("request via mmf"));
    Console.WriteLine($"[Client] Response: {resp.Message}");
  }

  public record Ping(string Message);

  public record Pong(string Message);
}
#endif
