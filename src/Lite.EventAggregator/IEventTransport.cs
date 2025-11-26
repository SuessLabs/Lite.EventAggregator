// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Lite.EventAggregator;

/// <summary>
///   Bi-directional event transport interface used for sending and receiving event packages, AKA: "envelopes".
/// </summary>
public interface IEventTransport
{
  /// <summary>Gets the reply address other parties should use to send responses back to this process.</summary>
  string ReplyAddress { get; }

  /// <summary>
  ///   Send a message (request or response). If envelope.IsResponse == true and envelope.ReplyTo != null,
  ///   transport sends to the reply channel; otherwise to its configured request channel.
  /// </summary>
  /// <param name="envelope">Payload envelope.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>Task.</returns>
  /// <remarks>Consider bringing back `SendAsync<TEvent>(..)` along side this bi-directional sender.</remarks>
  Task SendAsync(EventEnvelope envelope, CancellationToken cancellationToken = default);

  /// <summary>
  ///   Start listening for both requests and responses (as applicable).
  /// </summary>
  /// <param name="onMessageAsync">Message handler.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>Task.</returns>
  /// <remarks>Consider bringing back `SendAsync<TEvent>(..)` along side this bi-directional receiver.</remarks>
  Task StartAsync(Func<EventEnvelope, Task> onMessageAsync, CancellationToken cancellationToken = default);
}
