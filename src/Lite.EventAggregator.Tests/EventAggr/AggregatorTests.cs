// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Lite.EventAggregator.Tests.Models;

namespace Lite.EventAggregator.Tests.EventAggr;

[TestClass]
public class AggregatorTests
{
  [TestMethod]
  public void BasicEventTest()
  {
    var eventAggregator = new EventAggregator();

    // Subscribe
    UserCreatedEvent? received = null;
    eventAggregator.Subscribe<UserCreatedEvent>(evt => received = evt);

    // Publish
    eventAggregator.Publish(new UserCreatedEvent { UserName = "Damian" });
    Assert.IsNotNull(received);
    Assert.AreEqual("Damian", received.UserName);
  }

  [TestMethod]
  public async Task LocalPublishSuccessAsync()
  {
    var agg = new EventAggregator();
    string? seen = null;
    agg.Subscribe<Ping>(p => seen = p.Message);

    await agg.PublishAsync(new Ping("hello"));
    Assert.AreEqual("hello", seen);
  }

  [TestMethod]
  public async Task LocalRequestSuccessAsync()
  {
    const string sendMsg = "hi";
    const string recvMsg = "HI";

    var agg = new EventAggregator();
    agg.SubscribeRequest<Ping, Pong>(req => Task.FromResult(new Pong(req.Message.ToUpperInvariant())));

    // NOTE: No timeout provided
    var resp = await agg.RequestAsync<Ping, Pong>(new Ping(sendMsg));
    Assert.AreEqual(recvMsg, resp.Message);
  }

  [TestMethod]
  public async Task RequestTimeoutsThrowsAsync()
  {
    // Use case:
    //  There is no subscription found so if falls through
    //  This is usually reserved for an IPC receipted event (`_ipcEnvelopeTransport`)
    var agg = new EventAggregator();
    await Assert.ThrowsAsync<TimeoutException>(async () =>
        await agg.RequestAsync<Ping, Pong>(
          new Ping("hi"),
          timeout: TimeSpan.FromMilliseconds(50),
          System.Threading.CancellationToken.None));
  }

  [TestMethod]
  public async Task RequestWithoutTimeoutsTrowsAsync()
  {
    // Use case:
    //  There is no local subscription, no IPC, or timeout assigned
    var agg = new EventAggregator();
    await Assert.ThrowsAsync<TimeoutException>(async () =>
        await agg.RequestAsync<Ping, Pong>(
          new Ping("hi"),
          timeout: null,
          System.Threading.CancellationToken.None));
  }
}
