// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.
#if PREVIEW

using System;
using System.Threading.Tasks;
using Lite.EventAggregator.Tests.Models;
using Lite.EventAggregator.Transporter;

namespace Lite.EventAggregator.Tests.IpcReceiptedTransporters;

[TestClass]
[DoNotParallelize]
public class NamedPipesTests : BaseTestClass
{
  [TestMethod]
  public async Task Request_Response_Via_NamedPipesAsync()
  {
    var client = new EventAggregator();
    var server = new EventAggregator();

    var serverTransport = new NamedPipeEnvelopeTransport(
      incomingPipeName: "server-requests-in",
      outgoingPipeName: "client-requests-in",
      replyPipeName: "server-replies-in");

    var clientTransport = new NamedPipeEnvelopeTransport(
      incomingPipeName: "client-requests-in",
      outgoingPipeName: "server-requests-in",
      replyPipeName: "client-replies-in");

    await server.UseIpcEnvelopeTransportAsync(serverTransport);
    await client.UseIpcEnvelopeTransportAsync(clientTransport);

    server.SubscribeRequest<Ping, Pong>(req => Task.FromResult(new Pong(req.Message + " server")));

    var resp = await client.RequestAsync<Ping, Pong>(
      new Ping("hello"),
      timeout: TimeSpan.FromMilliseconds(DefaultTimeout),
      TestContext.CancellationToken);

    Assert.AreEqual("hello server", resp.Message);
  }
}
#endif
