// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System.Runtime.Versioning;
using System.Threading.Tasks;
using Lite.EventIpc.IpcTransport;
using Lite.EventIpc.Tests.Models;
using Microsoft.Extensions.Logging;

namespace Lite.EventIpc.Tests.IpcOneWayTransporter;

[SupportedOSPlatform("windows")]
[TestClass]
public class MemoryMappedTests : BaseTestClass
{
  private const string MapName = "test-map";

  [TestInitialize]
  public void CleanupTestInitialize()
  {
    _logger = CreateConsoleLogger<EventAggregator>(LogLevel.Debug);
  }

  [TestMethod]
  public void OneWayMemoryMapCanceledTest()
  {
    const string ExpectedUserName = "Hello";

    var server = new MemoryMappedTransport(MapName);
    var client = new MemoryMappedTransport(MapName);

    bool msgReceived = false;

    server.StartListening<UserCreatedEvent>(evt =>
    {
      Assert.AreEqual(ExpectedUserName, evt.UserName);
      msgReceived = true;
    });

    server.StopListening();
    client.Send(new UserCreatedEvent { UserName = ExpectedUserName });

    Assert.IsFalse(msgReceived);
  }

  [TestMethod]
  public void OneWayMemoryMapTest()
  {
    const string ExpectedUserName = "Hello";

    var server = new MemoryMappedTransport(MapName);
    var client = new MemoryMappedTransport(MapName);

    bool msgReceived = false;

    server.StartListening<UserCreatedEvent>(evt =>
    {
      Assert.AreEqual(ExpectedUserName, evt.UserName);
      msgReceived = true;
    });

    client.Send(new UserCreatedEvent { UserName = ExpectedUserName });

    // Give it a moment
    Task.Delay(5).Wait();
    Assert.IsTrue(msgReceived);
  }

  [TestMethod]
  [Ignore("This methodogoly is not implemented yet.")]
  public void VNextTcpTransportTest()
  {
    var msgPayload = "hello";
    var msgReceived = false;

    var server = new EventAggregator();
    var client = new EventAggregator();

    var serverIpcTransport = new MemoryMappedTransport(MapName);
    var clientIpcTransport = new MemoryMappedTransport(MapName);

    server.UseIpcTransport(serverIpcTransport);
    client.UseIpcTransport(clientIpcTransport);

    // Server listener
    server.Subscribe<Ping>(req =>
    {
      if (req.Message == msgPayload)
        msgReceived = true;
    });

    // Client sender
    client.Publish(new Ping(msgPayload));

    // Give it a moment
    Task.Delay(DefaultTimeout).Wait();
    Assert.IsTrue(msgReceived);
  }
}
