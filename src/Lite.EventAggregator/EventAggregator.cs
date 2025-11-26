// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Lite.EventAggregator;

public class EventAggregator : IEventAggregator
{
  private readonly ConcurrentDictionary<Type, List<WeakReference>> _subscribers = new();

  public void Subscribe<TEvent>(Action<TEvent> handler)
  {
    var eventType = typeof(TEvent);
    var weakHandler = new WeakReference(handler);

    _subscribers.AddOrUpdate(eventType,
      _ => new List<WeakReference> { weakHandler },
      (_, handlers) =>
      {
        handlers.Add(weakHandler);
        return handlers;
      });
  }

  public void Unsubscribe<TEvent>(Action<TEvent> handler)
  {
    var eventType = typeof(TEvent);
    if (_subscribers.TryGetValue(eventType, out var handlers))
      handlers.RemoveAll(wr => wr.Target is Action<TEvent> h && h == handler);
  }

  public void Publish<TEvent>(TEvent eventData)
  {
    var eventType = typeof(TEvent);
    if (_subscribers.TryGetValue(eventType, out var handlers))
    {
      var deadRefs = new List<WeakReference>();

      foreach (var weakRef in handlers)
      {
        if (weakRef.Target is Action<TEvent> handler)
          handler(eventData);
        else
          deadRefs.Add(weakRef);
      }

      // Clean up dead references
      foreach (var dead in deadRefs)
        handlers.Remove(dead);
    }
  }
}
