// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System;
using Lite.EventIpc.Tests.Models;
using Microsoft.Extensions.Logging;

namespace Lite.EventIpc.Tests.LocalEvents;

[TestClass]
public class WeakReferenceTests : BaseTestClass
{
  private static bool _received = false;
  private IEventAggregator _eventAggregator = null!;

  [TestInitialize]
  public void CleanupTestInitialize()
  {
    _logger = CreateConsoleLogger<EventAggregator>(LogLevel.Trace);
    _eventAggregator = new EventAggregator(_logger);
  }

  [TestMethod]
  public void WeakReferenceTest()
  {
    // Non-weak reference subscription for comparison
    //// eventAggregator.Subscribe<UserCreatedEvent>(e => _received = true);

    // If subscriber goes out of scope and GC runs, handler will be removed automatically
    var subscriber = new Subscriber(_eventAggregator);

    // Publish
    _eventAggregator.Publish(new UserCreatedEvent { UserName = "Damian" });

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
