// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System;

namespace Lite.EventAggregator.Transporter;

/// <summary>
///   Bi-directional event transport interface used for sending and receiving event packages, AKA: "envelopes".
/// </summary>
public interface IEventTransport
{
  /// <summary>
  ///   Send a message (request or response). If envelope.IsResponse == true and envelope.ReplyTo != null,
  ///   transport sends to the reply channel; otherwise to its configured request channel.
  /// </summary>
  /// <typeparam name="TEvent">Type of event data.</typeparam>
  /// <param name="eventData">Event data to send.</param>
  void Send<TEvent>(TEvent eventData);

  /// <summary>
  ///   Start listening for both requests and responses (as applicable).
  /// </summary>
  /// <typeparam name="TEvent">Type of event data.</typeparam>
  /// <param name="onEventReceived">Message handler.</param>
  void StartListening<TEvent>(Action<TEvent> onEventReceived);
}
