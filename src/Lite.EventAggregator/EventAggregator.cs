// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Lite.EventAggregator;

public class EventAggregator : IEventAggregator
{
  private readonly ConcurrentDictionary<Type, List<WeakReference>> _subscribers = new();
  private IEventTransport? _transport;

  public void EnableIpc(IEventTransport transport)
  {
    _transport = transport;
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

      foreach (var dead in deadRefs)
        handlers.Remove(dead);
    }

    // Send to IPC transport if enabled
    _transport?.Send(eventData);
  }

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

    // If IPC is enabled, start listening for remote events
    _transport?.StartListening<TEvent>(handler);
  }

  public void Unsubscribe<TEvent>(Action<TEvent> handler)
  {
    var eventType = typeof(TEvent);
    if (_subscribers.TryGetValue(eventType, out var handlers))
      handlers.RemoveAll(wr => wr.Target is Action<TEvent> h && h == handler);
  }
}
