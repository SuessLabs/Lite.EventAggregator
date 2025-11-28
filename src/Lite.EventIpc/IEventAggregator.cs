// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Lite.EventIpc.IpcReceiptTransport;
using Lite.EventIpc.IpcTransport;

namespace Lite.EventIpc;

public interface IEventAggregator
{
  /// <summary>Publish event async.</summary>
  /// <typeparam name="TEvent">Event Type.</typeparam>
  /// <param name="eventData">Event object.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>Task.</returns>
  Task PublishAsync<TEvent>(TEvent eventData, CancellationToken cancellationToken = default);

  /// <summary>Publishes the specified event to all registered subscribers.</summary>
  /// <remarks>
  ///   Subscribers must be registered to receive events of type <typeparamref name="TEvent"/>. This
  ///   method does not guarantee delivery order or that all subscribers will process the event synchronously.
  /// </remarks>
  /// <typeparam name="TEvent">The type of the event data to publish. Typically represents the event payload or message.</typeparam>
  /// <param name="eventData">The event data to be published to subscribers. Cannot be null.</param>
  void Publish<TEvent>(TEvent eventData);

  Task<TResponse> RequestAsync<TRequest, TResponse>(TRequest request, TimeSpan? timeout = null, CancellationToken cancellationToken = default);

  /// <summary>
  ///   Registers a handler to be invoked when an event of the specified type is published.
  /// </summary>
  /// <remarks>
  ///   Handlers are held using weak references to prevent memory leaks. If the handler is garbage
  ///   collected, it will no longer be invoked. Multiple handlers can be registered for the same event type.
  /// </remarks>
  /// <typeparam name="TEvent">The type of event to subscribe to. The handler will be invoked for events of this type.</typeparam>
  /// <param name="handler">The action to execute when an event of type <typeparamref name="TEvent"/> is published. Cannot be null.</param>
  void Subscribe<TEvent>(Action<TEvent> handler);

  /// <summary>
  ///   Registers a handler to process incoming requests of the specified type asynchronously.
  /// </summary>
  /// <remarks>
  ///   Multiple handlers can be registered for the same request type. Handlers are held using weak
  ///   references; if a handler is garbage collected, it will no longer be invoked.
  /// </remarks>
  /// <typeparam name="TRequest">The type of the request message to subscribe to.</typeparam>
  /// <typeparam name="TResponse">The type of the response message returned by the handler.</typeparam>
  /// <param name="handler">
  ///   A function that handles requests of type <typeparamref name="TRequest"/> and returns a
  ///   <see cref="Task{TResponse}"/> representing the asynchronous response.
  /// </param>
  void SubscribeRequest<TRequest, TResponse>(Func<TRequest, Task<TResponse>> handler);

  /// <summary>
  ///   Removes the specified event handler from the subscription list for the given event type.
  /// </summary>
  /// <remarks>
  ///   If the specified handler was not previously subscribed, this method has no effect. This method is
  ///   not thread-safe; concurrent modifications to the subscription list may result in undefined behavior.
  /// </remarks>
  /// <typeparam name="TEvent">The type of event to unsubscribe from.</typeparam>
  /// <param name="handler">
  ///   The delegate that was previously registered to handle events of type <typeparamref name="TEvent"/>. Cannot be null.
  /// </param>
  void Unsubscribe<TEvent>(Action<TEvent> handler);

  /// <summary>
  ///   Removes a previously registered request handler for the specified request and response types.
  /// </summary>
  /// <remarks>
  ///   If the specified handler is not currently subscribed, this method has no effect.
  ///   This method is thread-safe.
  /// </remarks>
  /// <typeparam name="TRequest">The type of the request message that the handler processes.</typeparam>
  /// <typeparam name="TResponse">The type of the response message returned by the handler.</typeparam>
  /// <param name="handler">
  ///   The handler delegate to unsubscribe. Must match a previously registered handler for the given request and response types.
  /// </param>
  void UnsubscribeRequest<TRequest, TResponse>(Func<TRequest, Task<TResponse>> handler);

  /// <summary>
  ///   Option to include bi-directional IPC support.
  ///   Configures and starts the event envelope transport to be used for bi-directional asynchronous delivery.
  ///   NOTE:
  ///     1) Only one transport can be used at a time.
  ///     2) This method must be called before sending or receiving.
  ///     3) This method replaces any previously configured IPC transport (<see cref="UseIpcTransport(IEventTransport)"/>).
  /// </summary>
  /// <param name="envelopeTransport">The IPC event envelope transport to use for receiving and processing messages. Cannot be null.</param>
  /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
  /// <returns>A task that represents the asynchronous operation of starting the transport.</returns>
  Task UseIpcEnvelopeTransportAsync(IEventEnvelopeTransport envelopeTransport, CancellationToken cancellationToken = default);

  /// <summary>
  ///   Option to include one-way IPC support.
  ///   Configures the event system to use the specified IPC transport for event delivery.
  ///   NOTE:
  ///     1) Only one transport can be used at a time.
  ///     2) This method must be called before sending or receiving.
  ///     3) This method replaces any previously configured envelope transport (<see cref="UseIpcEnvelopeTransportAsync(IEventEnvelopeTransport, CancellationToken)"/>).
  /// </summary>
  /// <remarks>
  ///   Calling this method replaces any previously configured event transport with the specified
  ///   instance. This method should be called before starting event processing to ensure correct
  ///   transport behavior.
  /// </remarks>
  /// <param name="transport">The IPC transport implementation to be used for sending and receiving events. Cannot be null.</param>
  public void UseIpcTransport(IEventTransport transport);
}
