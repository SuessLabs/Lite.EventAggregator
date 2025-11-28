// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System;
using System.Net;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Lite.EventAggregator.Tests.Models;
using Lite.EventAggregator.Transporter;

namespace Lite.EventAggregator.Tests.IpcOneWayTransporter;

[SupportedOSPlatform("windows")]
[TestClass]
public class MemoryMappedTests : BaseTestClass
{
  private const string MapName = "test-map";

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
