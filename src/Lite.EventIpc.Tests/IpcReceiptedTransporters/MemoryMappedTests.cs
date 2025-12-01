// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.
#if PREVIEW

using System;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Lite.EventIpc.IpcReceiptTransport;
using Lite.EventIpc.Tests.Models;
using Microsoft.Extensions.Logging;

namespace Lite.EventIpc.Tests.IpcReceiptedTransporters;

[SupportedOSPlatform("windows")]
[TestClass]
public class MemoryMappedTests : BaseTestClass
{
  private const string MapRequestName = "ipc-req-map";
  private const string MapResponseName = "ipc-resp-map";
  private const string SignalRequestName = "ipc-req-signal";
  private const string SignalResponseName = "ipc-resp-signal";

  [TestInitialize]
  public void CleanupTestInitialize()
  {
    _logger = CreateConsoleLogger<EventAggregator>(LogLevel.Trace);
  }

  [TestMethod]
  public async Task Request_Response_Via_MemoryMappedAsync()
  {
    const string MsgRequest = "hello";
    const string MsgResponse = " mmf";

    var client = new EventAggregator(_logger);
    var server = new EventAggregator(_logger);

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

    bool msgReceived = false;
    ////server.SubscribeRequest<Ping, Pong>(async (req) =>
    ////{
    ////  _logger?.LogInformation("Test Subscriber received Ping, returning Pong..");
    ////  msgReceived = true;
    ////  await Task.Yield();
    ////  return new Pong(req.Message + MsgResponse);
    ////});

    // TODO: We need to trigger a cancellation after sending this back, and set
    //       that the transmission was a success
    // TODO-2: Gotta return without using "Task.FromResult" that's lame!
    server.SubscribeRequest<Ping, Pong>(req =>
    {
      _logger?.LogInformation("Test Subscriber received Ping, returning Pong..");
      msgReceived = true;
      return Task.FromResult(new Pong(req.Message + MsgResponse));
    });

    _logger?.LogInformation("Test Sending Ping...");
    var resp = await client.RequestAsync<Ping, Pong>(
      new Ping(MsgRequest),
      timeout: TimeSpan.FromMilliseconds(3_000));

    _logger?.LogInformation("Test Msg Received: {WasRcv}", msgReceived);
    _logger?.LogInformation("Test Response is null: {IsNull}", resp is null ? "NULL" : "HasValue");
    Task.Delay(DefaultTimeout).Wait();

    Assert.IsTrue(msgReceived);
    Assert.AreEqual(MsgRequest + MsgResponse, resp.Message);
  }
}

#endif
