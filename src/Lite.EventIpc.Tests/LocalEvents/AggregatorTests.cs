// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Lite.EventIpc.Tests.Models;
using Microsoft.Extensions.Logging;

namespace Lite.EventIpc.Tests.LocalEvents;

[TestClass]
public class AggregatorTests : BaseTestClass
{
  private EventAggregator _eventAggregator;

  public AggregatorTests()
  {
    _eventAggregator = null!; // Suppress not initialized warning
  }

  [TestInitialize]
  public void CleanupTestInitialize()
  {
    _logger = CreateConsoleLogger<EventAggregator>(LogLevel.Trace);
    _eventAggregator = new EventAggregator(_logger);
  }

  [TestMethod]
  public async Task LocalEventRequestWithResponseSuccessAsync()
  {
    const string sendMsg = "hi";
    const string recvMsg = "HI";

    _eventAggregator.SubscribeRequest<Ping, Pong>(req => Task.FromResult(new Pong(req.Message.ToUpperInvariant())));

    // NOTE: No timeout provided
    var resp = await _eventAggregator.RequestAsync<Ping, Pong>(new Ping(sendMsg));
    Assert.AreEqual(recvMsg, resp.Message);
  }

  [TestMethod]
  public void LocalEventTest()
  {
    // Subscribe
    UserCreatedEvent? received = null;
    _eventAggregator.Subscribe<UserCreatedEvent>(evt => received = evt);

    // Publish
    _eventAggregator.Publish(new UserCreatedEvent { UserName = "Damian" });
    Assert.IsNotNull(received);
    Assert.AreEqual("Damian", received.UserName);
  }

  [TestMethod]
  public async Task LocalPublishSuccessAsync()
  {
    string? seen = null;
    _eventAggregator.Subscribe<Ping>(p => seen = p.Message);

    await _eventAggregator.PublishAsync(new Ping("hello"));
    Assert.AreEqual("hello", seen);
  }

  [TestMethod]
  public async Task LocalPublishWithoutLoggingSuccessAsync()
  {
    // NOTE: Do not pass in '_logger' into constructor
    var agg = new EventAggregator();

    string? seen = null;
    agg.Subscribe<Ping>(p => seen = p.Message);

    await agg.PublishAsync(new Ping("hello"));
    Assert.AreEqual("hello", seen);
  }

  [TestMethod]
  public async Task RequestTimeoutsThrowsAsync()
  {
    // Use case:
    //  There is no subscription found so if falls through
    //  This is usually reserved for an IPC receipted event (`_ipcEnvelopeTransport`)
    await Assert.ThrowsAsync<TimeoutException>(async () =>
        await _eventAggregator.RequestAsync<Ping, Pong>(
          new Ping("hi"),
          timeout: TimeSpan.FromMilliseconds(DefaultTimeout),
          System.Threading.CancellationToken.None));
  }

  [TestMethod]
  public async Task RequestWithoutTimeoutsTrowsAsync()
  {
    // Use case:
    //  There is no local subscription, no IPC, or timeout assigned
    await Assert.ThrowsAsync<TimeoutException>(async () =>
        await _eventAggregator.RequestAsync<Ping, Pong>(
          new Ping("hi"),
          timeout: null,
          System.Threading.CancellationToken.None));
  }
}
