// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.
#if PREVIEW

using System;
using System.Net;
using System.Threading.Tasks;
using Lite.EventAggregator.IpcReceiptTransport;
using Lite.EventAggregator.Tests.Models;

namespace Lite.EventAggregator.Tests.IpcReceiptedTransporters;

[TestClass]
[DoNotParallelize]
public class TcpTransportTests : BaseTestClass
{
  [TestMethod]
  [DoNotParallelize]
  public async Task Request_Response_Via_TcpAsync()
  {
    var client = new EventAggregator();
    var server = new EventAggregator();

    var serverTransport = new TcpEnvelopeTransport(
      requestListen: new IPEndPoint(IPAddress.Loopback, 6001),
      replyListen: new IPEndPoint(IPAddress.Loopback, 6002),
      requestSend: new IPEndPoint(IPAddress.Loopback, 6003),
      replySend: new IPEndPoint(IPAddress.Loopback, 6004));

    var clientTransport = new TcpEnvelopeTransport(
      requestListen: new IPEndPoint(IPAddress.Loopback, 6003),
      replyListen: new IPEndPoint(IPAddress.Loopback, 6004),
      requestSend: new IPEndPoint(IPAddress.Loopback, 6001),
      replySend: new IPEndPoint(IPAddress.Loopback, 6002));

    await server.UseIpcEnvelopeTransportAsync(serverTransport);
    await client.UseIpcEnvelopeTransportAsync(clientTransport);

    server.SubscribeRequest<Ping, Pong>(req => Task.FromResult(new Pong(req.Message + " tcp")));

    var resp = await client.RequestAsync<Ping, Pong>(
      new Ping("hello"),
      timeout: TimeSpan.FromMilliseconds(DefaultTimeout),
      TestContext.CancellationToken);

    Assert.AreEqual("hello tcp", resp.Message);
  }
}

#endif
