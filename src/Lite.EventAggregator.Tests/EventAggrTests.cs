// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using Lite.EventAggregator.Tests.Events;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lite.EventAggregator.Tests;

[TestClass]
public sealed class EventAggrTests
{
  [TestMethod]
  public void TestMethod1()
  {
    bool received = false;

    var eventAggregator = new EventAggregator();

    // Subscribe
    eventAggregator.Subscribe<UserCreatedEvent>(e =>
    {
      received = true;
    });

    // Publish
    eventAggregator.Publish(new UserCreatedEvent { UserName = "Damian" });

    Assert.IsTrue(received);
  }
}
