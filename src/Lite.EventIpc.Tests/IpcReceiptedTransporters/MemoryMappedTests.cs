// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.
#if PREVIEW

using System;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Lite.EventAggregator.Tests.Models;
using Lite.EventAggregator.Transporter;

namespace Lite.EventAggregator.Tests.IpcReceiptedTransporters;

[SupportedOSPlatform("windows")]
[TestClass]
public class MemoryMappedTests : BaseTestClass
{
  private const string MapRequestName = "ipc-req-map";
  private const string MapResponseName = "ipc-resp-map";
  private const string SignalRequestName = "ipc-req-signal";
  private const string SignalResponseName = "ipc-resp-signal";

  [TestMethod]
  public async Task Request_Response_Via_MemoryMappedAsync()
  {
    const string PayloadRequest = "hello";
    const string PayloadResponse = " mmf";

    var client = new EventAggregator();
    var server = new EventAggregator();

    var serverTransport = new MemoryMappedEnvelopeTransport(
      requestMapName: MapRequestName,
      responseMapName: MapResponseName,
      requestSignalName: SignalRequestName,
      responseSignalName: SignalResponseName);

    var clientTransport = new MemoryMappedEnvelopeTransport(
      requestMapName: MapRequestName,
      responseMapName: MapResponseName,
      requestSignalName: SignalRequestName,
      responseSignalName: SignalResponseName);

    await server.UseIpcEnvelopeTransportAsync(serverTransport);
    await client.UseIpcEnvelopeTransportAsync(clientTransport);

    bool wasReceived = false;
    server.SubscribeRequest<Ping, Pong>(req =>
    {
      Console.WriteLine("Subscriber received Ping, returning Pong..");
      wasReceived = true;
      return Task.FromResult(new Pong(req.Message + PayloadResponse));
    });

    Console.WriteLine("Sending Ping...");
    var resp = await client.RequestAsync<Ping, Pong>(
      new Ping(PayloadRequest),
      timeout: TimeSpan.FromMilliseconds(DefaultTimeout200),
      TestContext.CancellationToken);

    Console.WriteLine("Receiver recognized: " + wasReceived);
    Console.WriteLine("Response is null: " + resp is null);
    Task.Delay(DefaultTimeout).Wait();

    Assert.IsTrue(wasReceived);
    Assert.AreEqual(PayloadRequest + PayloadResponse, resp.Message);
  }
}
#endif
