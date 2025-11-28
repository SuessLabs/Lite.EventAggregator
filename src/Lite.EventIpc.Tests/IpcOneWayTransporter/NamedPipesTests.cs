// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Lite.EventIpc.IpcTransport;
using Lite.EventIpc.Tests.Models;

namespace Lite.EventIpc.Tests.IpcOneWayTransporter;

[TestClass]
[DoNotParallelize]
public class NamedPipesTests : BaseTestClass
{
  private string _msgPayload = "hello";
  private bool _msgReceived = false;

  [TestInitialize]
  public void CleanupTestInitialize()
  {
    _msgReceived = false;
  }

  [TestMethod]
  [DoNotParallelize]
  public void NamedPipeTest()
  {
    var serverTransport = new NamedPipeTransport("server-requests-in");
    var clientTransport = new NamedPipeTransport("server-requests-in");

    serverTransport.StartListening<Ping>(req =>
    {
      if (req.Message == _msgPayload)
        _msgReceived = true;
    });

    serverTransport.Send<Ping>(new Ping(_msgPayload));

    // Give it a moment
    Task.Delay(DefaultTimeout200, TestContext.CancellationToken).Wait();

    Assert.IsTrue(_msgReceived);
  }

  [TestMethod]
  [Ignore("This methodogoly is not implemented yet.")]
  public void VNextNamedPipeTest()
  {
    var client = new EventAggregator();
    var server = new EventAggregator();

    var serverTransport = new NamedPipeTransport("server-requests-in");
    var clientTransport = new NamedPipeTransport("server-requests-in");

    server.UseIpcTransport(serverTransport);
    client.UseIpcTransport(clientTransport);

    server.Subscribe<Ping>(req =>
    {
      if (req.Message == _msgPayload)
        _msgReceived = true;
    });

    client.Publish<Ping>(new Ping(_msgPayload));

    Assert.IsTrue(_msgReceived);
  }
}
