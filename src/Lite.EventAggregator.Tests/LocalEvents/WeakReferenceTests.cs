// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System;
using Lite.EventAggregator.Tests.Models;

namespace Lite.EventAggregator.Tests.LocalEvents;

[TestClass]
public class WeakReferenceTests : BaseTestClass
{
  private static bool _received = false;

  [TestMethod]
  public void WeakReferenceTest()
  {
    var eventAggregator = new EventAggregator();

    // Non-weak reference subscription for comparison
    //// eventAggregator.Subscribe<UserCreatedEvent>(e => _received = true);

    // If subscriber goes out of scope and GC runs, handler will be removed automatically
    var subscriber = new Subscriber(eventAggregator);

    // Publish
    eventAggregator.Publish(new UserCreatedEvent { UserName = "Damian" });

    Assert.IsTrue(_received);
    Assert.IsNotNull(subscriber);
  }

  public class Subscriber
  {
    public Subscriber(IEventAggregator aggregator)
    {
      aggregator.Subscribe<UserCreatedEvent>(OnUserCreated);
    }

    private void OnUserCreated(UserCreatedEvent e)
    {
      _received = true;
      Console.WriteLine($"User created: {e.UserName}");
    }
  }
}
