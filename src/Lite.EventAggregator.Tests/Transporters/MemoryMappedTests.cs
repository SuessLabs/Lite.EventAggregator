// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System.Runtime.Versioning;
using System.Threading.Tasks;
using Lite.EventAggregator.Transporter;

namespace Lite.EventAggregator.Tests;

[SupportedOSPlatform("windows")]
[TestClass]
public class MemoryMappedTests
{
  [TestMethod]
  public async Task Request_Response_Via_MemoryMappedAsync()
  {
    var client = new EventAggregator();
    var server = new EventAggregator();

    var serverTransport = new MemoryMappedEnvelopeTransport(
      requestMapName: "req-map",
      responseMapName: "resp-map",
      requestSignalName: "req-signal",
      responseSignalName: "resp-signal");

    var clientTransport = new MemoryMappedEnvelopeTransport(
      requestMapName: "req-map",
      responseMapName: "resp-map",
      requestSignalName: "req-signal",
      responseSignalName: "resp-signal");

    await server.UseIpcEnvelopeTransportAsync(serverTransport);
    await client.UseIpcEnvelopeTransportAsync(clientTransport);

    server.SubscribeRequest<Ping, Pong>(req => Task.FromResult(new Pong(req.Message + " mmf")));

    var resp = await client.RequestAsync<Ping, Pong>(new Ping("hello"));
    Assert.AreEqual("hello mmf", resp.Message);
  }
}
