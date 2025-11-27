// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Lite.EventAggregator;

/// <summary>
///   Provides a central hub for publishing events and handling request/response messaging between loosely coupled
///   components, supporting both local and remote event delivery.
/// </summary>
/// <remarks>
///   The EventAggregator enables decoupled communication by allowing components to subscribe to events or
///   requests without direct references. It supports asynchronous event publishing and request/response patterns, and can
///   be integrated with an external transport for inter-process or networked messaging. Subscribers are managed using
///   weak references to prevent memory leaks. Thread safety is ensured for all public operations. This class is suitable
///   for scenarios such as implementing event-driven architectures, CQRS, or distributed systems where components need to
///   communicate without tight coupling.
/// </remarks>
public class EventAggregator : IEventAggregator
{
  private readonly ConcurrentDictionary<Type, List<WeakReference>> _eventSubscribers = new();
  private readonly ConcurrentDictionary<string, TaskCompletionSource<object?>> _pendingRequests = new();
  private readonly ConcurrentDictionary<Type, List<WeakReference>> _requestSubscribers = new();

  /// <summary>Bi-directional IPC transporter.</summary>
  private IEventEnvelopeTransport? _ipcEnvelopeTransport;

  /// <summary>Single direction IPC transporter.</summary>
  private IEventTransport? _ipcTransport;

  /// <inheritdoc/>
  public async Task PublishEnvelopeAsync<TEvent>(TEvent eventData, CancellationToken cancellationToken = default)
  {
    // TODO: Combine with Publish<TEvent>, so we only need to maintain one.
    // Local dispatch
    DispatchEventLocal(eventData);

    // Remote dispatch
    if (_ipcEnvelopeTransport is not null)
    {
      var envelope = EventSerializer.Wrap(eventData, isRequest: false, replyTo: null);
      await _ipcEnvelopeTransport.SendAsync(envelope, cancellationToken);
    }
    ////else if (_ipcTransport is not null)
    ////{
    ////  // Send to IPC transport if enabled
    ////  _ipcTransport?.Send(eventData);
    ////}
  }

  /// <inheritdoc/>
  public void Publish<TEvent>(TEvent eventData)
  {
    var eventType = typeof(TEvent);
    if (_eventSubscribers.TryGetValue(eventType, out var handlers))
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
    _ipcTransport?.Send(eventData);
  }

  /// <inheritdoc/>
  /// <remarks>Bi-directional transport only.</remarks>
  public async Task<TResponse> RequestAsync<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken = default)
  {
    var correlationId = Guid.NewGuid().ToString("N");
    var tcs = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
    _pendingRequests[correlationId] = tcs;

    // Try local handlers first (if any) – we invoke first; if handled locally, short-circuit (no IPC)
    var localHandler = GetFirstRequestHandler<TRequest, TResponse>();
    if (localHandler != null)
    {
      var localResponse = await localHandler(request);
      _pendingRequests.TryRemove(correlationId, out _);
      return localResponse;
    }

    // NOTE: This method requires bi-directional transport
    if (_ipcEnvelopeTransport == null)
      throw new InvalidOperationException("No transport configured for request/response.");

    var envelope = EventSerializer.Wrap(request, isRequest: true, replyTo: _ipcEnvelopeTransport.ReplyAddress, correlationId);
    await _ipcEnvelopeTransport.SendAsync(envelope, cancellationToken);

    using (cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken)))
    {
      var obj = await tcs.Task.ConfigureAwait(false);
      if (obj is TResponse typed)
        return typed;

      // Try deserializing if the transport delivered JSON payload as string
      if (obj is string s)
      {
        try
        {
          return EventSerializer.Deserialize<TResponse>(s);
        }
        catch
        {
          /* fall through */
        }
      }

      throw new InvalidOperationException($"Response type mismatch for correlationId={correlationId}.");
    }
  }

  /// <inheritdoc/>
  public void Subscribe<TEvent>(Action<TEvent> handler)
  {
    var eventType = typeof(TEvent);
    var weakHandler = new WeakReference(handler);

    _eventSubscribers.AddOrUpdate(eventType,
      _ => new List<WeakReference> { weakHandler },
      (_, handlers) =>
    {
      handlers.Add(weakHandler);
      return handlers;
    });
  }

  /// <inheritdoc/>
  public void SubscribeRequest<TRequest, TResponse>(Func<TRequest, Task<TResponse>> handler)
  {
    var type = typeof(TRequest);
    var wr = new WeakReference(handler);
    _requestSubscribers.AddOrUpdate(type, _ => [wr], (_, list) =>
    {
      list.Add(wr);
      return list;
    });
  }

  /// <inheritdoc/>
  public void Unsubscribe<TEvent>(Action<TEvent> handler)
  {
    var eventType = typeof(TEvent);
    if (_eventSubscribers.TryGetValue(eventType, out var handlers))
    {
      //// handlers.RemoveAll(wr => wr.Target is Action<TEvent> h && h == handler);
      for (int i = handlers.Count - 1; i >= 0; i--)
      {
        var target = handlers[i].Target;
        if (target is Action<TEvent> existing && existing == handler)
          handlers.RemoveAt(i);
        else if (target is null)
          handlers.RemoveAt(i); // cleanup dead refs
      }
    }
  }

  /// <inheritdoc/>
  public void UnsubscribeRequest<TRequest, TResponse>(Func<TRequest, Task<TResponse>> handler)
  {
    var type = typeof(TRequest);
    if (_requestSubscribers.TryGetValue(type, out var handlers))
    {
      //// handlers.RemoveAll(w => w.Target is Func<TRequest, Task<TResponse>> h && h == handler);
      for (int i = handlers.Count - 1; i >= 0; i--)
      {
        var target = handlers[i].Target;
        if (target is Func<TRequest, Task<TResponse>> existing && existing == handler)
          handlers.RemoveAt(i);
        else if (target == null)
          handlers.RemoveAt(i); // cleanup dead refs
      }
    }
  }

  /// <inheritdoc/>
  public async Task UseIpcEnvelopeTransportAsync(IEventEnvelopeTransport transport, CancellationToken cancellationToken = default)
  {
    if (_ipcTransport is not null)
      _ipcTransport = null;

    _ipcEnvelopeTransport = transport;
    await _ipcEnvelopeTransport.StartAsync(OnTransportMessageAsync, cancellationToken);
  }

  /// <inheritdoc/>
  public void UseIpcTransport(IEventTransport transport)
  {
    if (_ipcEnvelopeTransport is not null)
      _ipcEnvelopeTransport = null;

    _ipcTransport = transport;
  }

  private void DeliverLocalGeneric<T>(T payload) => DispatchEventLocal(payload);

  private void DispatchEventLocal<TEvent>(TEvent eventData)
  {
    var type = typeof(TEvent);
    if (_eventSubscribers.TryGetValue(type, out var subs))
    {
      // Manual iteration; removing dead refs
      for (int i = subs.Count - 1; i >= 0; i--)
      {
        var target = subs[i].Target;
        if (target is Action<TEvent> handler)
          handler(eventData);
        else if (target == null)
          subs.RemoveAt(i);
      }

      /*
      var dead = new List<WeakReference>();
      foreach (var wr in subs)
      {
        if (wr.Target is Action<TEvent> handler)
          handler(eventData);
        else
          dead.Add(wr);
      }

      foreach (var d in dead)
        subs.Remove(d);
      */
    }
  }

  /// <summary>
  ///   Retrieves the first registered request handler delegate for the specified
  ///   request and response types, if available.
  /// </summary>
  /// <remarks>
  ///   If multiple handlers are registered for the specified request type, only the first available
  ///   handler is returned. Dead or collected handlers are automatically removed from the internal registry.
  /// </remarks>
  /// <typeparam name="TRequest">The type of the request parameter that the handler accepts.</typeparam>
  /// <typeparam name="TResponse">The type of the response returned by the handler as a task result.</typeparam>
  /// <returns>
  ///   A delegate that handles requests of type <typeparamref name="TRequest"/> and
  ///   returns a <see cref="Task{TResponse}"/> if a handler is registered; otherwise, <see langword="null"/>.
  /// </returns>
  private Func<TRequest, Task<TResponse>>? GetFirstRequestHandler<TRequest, TResponse>()
  {
    var type = typeof(TRequest);
    if (_requestSubscribers.TryGetValue(type, out var subs))
    {
      // Find the first live handler, clean dead ones
      Func<TRequest, Task<TResponse>>? found = null;
      for (int i = subs.Count - 1; i >= 0; i--)
      {
        // For now, keep scanning backwards to cleanup dead refs.
        // If this is problematic, we can exit early as we did in commit `825CCCD6`.
        var target = subs[i].Target;
        if (target is Func<TRequest, Task<TResponse>> handler)
          found = handler;
        else if (target == null)
          subs.RemoveAt(i);
      }

      return found;
    }

    return null;
  }

  /// <summary>
  ///   Processes an incoming transport message envelope, dispatching requests, responses,
  ///   or published events to the appropriate handlers.
  /// </summary>
  /// <remarks>
  ///   This method routes response messages to pending request handlers, invokes registered request
  ///   handlers for incoming requests, and delivers published events to local subscribers. If no matching handler is
  ///   found for a request, the message is ignored. The method does not throw exceptions for deserialization or handler
  ///   invocation errors; such messages are silently dropped.
  /// </remarks>
  /// <param name="envelope">The event envelope containing the serialized event data, type information, correlation identifiers, and routing metadata.</param>
  /// <returns>A task that represents the asynchronous operation of handling the transport message.</returns>
  private async Task OnTransportMessageAsync(EventEnvelope envelope)
  {
    // Resolve type
    var eventType = Type.GetType(envelope.EventType, throwOnError: false);
    if (eventType is null)
      return;

    if (envelope.IsResponse)
    {
      // Complete pending request
      // Aggregator caller will deserialize to expected TResponse
      if (_pendingRequests.TryRemove(envelope.CorrelationId, out var tcs))
        tcs.TrySetResult(envelope.PayloadJson);

      return;
    }

    // Request or Publish
    var payloadObj = JsonSerializer.Deserialize(envelope.PayloadJson, eventType);
    if (payloadObj is null)
      return;

    if (envelope.IsRequest)
    {
      // Find request handler and send response
      var handlerList = _requestSubscribers.TryGetValue(eventType, out var subs)
        ? subs
        : null;

      var handler = handlerList?.Select(w => w.Target).OfType<dynamic>().FirstOrDefault();

      // no handler – drop or log
      if (handler == null)
        return;

      object response;
      try
      {
        // Invoke dynamically
        response = await handler((dynamic)payloadObj);
      }
      catch
      {
        // For brevity, ignoring error propagation
        return;
      }

      if (_ipcEnvelopeTransport != null && envelope.ReplyTo != null)
      {
        var responseEnvelope = new EventEnvelope
        {
          MessageId = Guid.NewGuid().ToString("N"),
          CorrelationId = envelope.CorrelationId,
          EventType = response.GetType().AssemblyQualifiedName!,
          IsRequest = false,
          IsResponse = true,
          ReplyTo = envelope.ReplyTo, // used by transport to route back to sender
          Timestamp = DateTimeOffset.UtcNow,
          PayloadJson = EventSerializer.Serialize(response),
        };

        await _ipcEnvelopeTransport.SendAsync(responseEnvelope);
      }

      return;
    }

    // One-way publish – deliver locally
    // Use closed generic method
    var deliverMethod = typeof(EventAggregator).GetMethod(
      nameof(DeliverLocalGeneric),
      System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

    var closed = deliverMethod!.MakeGenericMethod(eventType);

    closed.Invoke(this, new[] { payloadObj });
  }
}
