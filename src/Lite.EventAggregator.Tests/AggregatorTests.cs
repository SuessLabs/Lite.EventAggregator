// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Lite.EventAggregator.Tests;

public record Ping(string Message);

public record Pong(string Message);

[TestClass]
public class AggregatorTests
{
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
    var agg = new EventAggregator();
    agg.SubscribeRequest<Ping, Pong>(req => Task.FromResult(new Pong(req.Message.ToUpperInvariant())));

    var resp = await agg.RequestAsync<Ping, Pong>(new Ping("hi"));
    Assert.AreEqual("HI", resp.Message);
  }

  [TestMethod]
  public async Task RequestTimeoutsAsync()
  {
    var agg = new EventAggregator();
    await Assert.ThrowsAsync<TimeoutException>(async () =>
        await agg.RequestAsync<Ping, Pong>(new Ping("hi"), timeout: TimeSpan.FromMilliseconds(50)));
  }
}
