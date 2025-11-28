// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Threading.Tasks;
using Lite.EventAggregator.Tests.Models;
using Lite.EventAggregator.Transporter;

namespace Lite.EventAggregator.Tests.IpcOneWayTransporter;

[TestClass]
public class TcpTransportTests : BaseTestClass
{
  private const int ReplyListenPort = 6004;
  private const int ReplySendPort = 6002;
  private const int RequestListenPort = 6003;
  private const int RequestSendPort = 6001;

  [TestMethod]
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
    var msgPayload = "hello";
    var msgReceived = false;

    var server = new EventAggregator();
    var client = new EventAggregator();

    var serverTransport = new TcpTransport(IPAddress.Loopback.ToString(), RequestSendPort);
    var clientTransport = new TcpTransport(IPAddress.Loopback.ToString(), RequestSendPort);

    server.UseIpcTransport(serverTransport);
    client.UseIpcTransport(clientTransport);

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
