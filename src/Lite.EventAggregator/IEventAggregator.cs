// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System;

namespace Lite.EventAggregator;

public interface IEventAggregator
{
  void Publish<TEvent>(TEvent eventData);

  void Subscribe<TEvent>(Action<TEvent> handler);

  void Unsubscribe<TEvent>(Action<TEvent> handler);

  /// <summary>Option to include IPC support.</summary>
  /// <param name="transport">IPC transportation mechanism.</param>
  void EnableIpc(IEventTransport transport);
}
