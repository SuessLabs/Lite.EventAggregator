// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Threading.Tasks;
using Lite.EventIpc.Tests.Models;
using Lite.EventIpc.IpcTransport;

namespace Lite.EventIpc.Tests.IpcOneWayTransporter;

[TestClass]
[DoNotParallelize]
public class TcpTransportTests : BaseTestClass
{
  private const int RequestSendPort = 6001;

  private string _msgPayload = "hello";
  private bool _msgReceived = false;

  [TestInitialize]
  public void CleanupTestInitialize()
  {
    _msgReceived = false;
  }

  [TestMethod]
  [DoNotParallelize]
  public void OneWayTcpIpcTransportTest()
  {
    var msgPayload = "hello";
    var msgReceived = false;

    var serverTransport = new TcpTransport(IPAddress.Loopback.ToString(), RequestSendPort);
    var clientTransport = new TcpTransport(IPAddress.Loopback.ToString(), RequestSendPort);

    // Server listener
    serverTransport.StartListening<Ping>(req =>
    {
      if (req.Message == msgPayload)
        msgReceived = true;
    });

    // Client sender
    clientTransport.Send(new Ping(msgPayload));

    // Give it a moment
    Task.Delay(DefaultTimeout).Wait();
    Assert.IsTrue(msgReceived);
  }

  [TestMethod]
  [Ignore("This methodogoly is not implemented yet.")]
  public void VNextTcpTransportTest()
  {
    var server = new EventAggregator();
    var client = new EventAggregator();

    var serverTransport = new TcpTransport(IPAddress.Loopback.ToString(), RequestSendPort);
    var clientTransport = new TcpTransport(IPAddress.Loopback.ToString(), RequestSendPort);

    server.UseIpcTransport(serverTransport);
    client.UseIpcTransport(clientTransport);

    // Server listener
    server.Subscribe<Ping>(req =>
    {
      if (req.Message == _msgPayload)
        _msgReceived = true;
    });

    // Client sender
    client.Publish(new Ping(_msgPayload));

    // Give it a moment
    Task.Delay(DefaultTimeout).Wait();
    Assert.IsTrue(_msgReceived);
  }
}
