// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System;

namespace Lite.EventAggregator;

public interface IEventTransport
{
  /// <summary>Send IPC event.</summary>
  /// <typeparam name="TEvent">Event type.</typeparam>
  /// <param name="eventData">Payload data.</param>
  void Send<TEvent>(TEvent eventData);

  /// <summary>Listen subscription for event.</summary>
  /// <typeparam name="TEvent">Event type.</typeparam>
  /// <param name="onEventReceived">Action when event is received.</param>
  void StartListening<TEvent>(Action<TEvent> onEventReceived);
}
