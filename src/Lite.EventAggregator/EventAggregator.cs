// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Lite.EventAggregator.Core;
using Lite.EventAggregator.Transporter;

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
  ////private readonly ConcurrentDictionary<Type, List<WeakReference>> _eventSubscribers = new();
  ////private readonly ConcurrentDictionary<string, TaskCompletionSource<object?>> _pendingRequests = new();
  ////private readonly ConcurrentDictionary<Type, List<WeakReference>> _requestSubscribers = new();

  private readonly TimeSpan _defaultTimeout = TimeSpan.FromSeconds(5);
  private readonly ConcurrentDictionary<Type, List<IWeakAction>> _eventHandlers = new();
  private readonly ConcurrentDictionary<string, PendingRequest> _pendingRequests = new();
  private readonly ConcurrentDictionary<Type, List<IRequestHandler>> _requestHandlers = new();

  /// <summary>Bi-directional IPC transporter.</summary>
  private IEventEnvelopeTransport? _ipcEnvelopeTransport;

  /// <summary>Single direction IPC transporter.</summary>
  private IEventTransport? _ipcTransport;

  /// <summary>Master switch if an IPC Transport is intended or not.</summary>
  /// <remarks>A local even can have a RequestAsync timeout, but not an IPC receipted transport.</remarks>
  private bool _usingIpcTransport = false;

  /// <inheritdoc/>
  public void Publish<TEvent>(TEvent eventData)
  {
    // Local dispatch
    DispatchEventLocal(eventData);

    // Send to IPC transport if enabled
    _ipcTransport?.Send(eventData);
  }

  /// <inheritdoc/>
  public async Task PublishAsync<TEvent>(TEvent eventData, CancellationToken cancellationToken = default)
  {
    // Local dispatch
    DispatchEventLocal(eventData);

    // Remote dispatch
    if (_ipcEnvelopeTransport is not null)
    {
      // Send to bi-directional IPC transport if enabled
      var envelope = EventSerializer.Wrap(eventData, isRequest: false, replyTo: null);
      await _ipcEnvelopeTransport.SendAsync(envelope, cancellationToken);
    }
    else if (_ipcTransport is not null)
    {
      // Send to one-way IPC transport if enabled
      _ipcTransport?.Send(eventData);
    }
  }

  /// <inheritdoc/>
  /// <remarks>Bi-directional transport only.</remarks>
  public async Task<TResponse> RequestAsync<TRequest, TResponse>(TRequest request, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
  {
    // Try to find local event subscription handler first and exists
    // NOTE: Consider separating IPC from local event request/response handling
    var local = GetFirstRequestHandler(typeof(TRequest));
    if (local is not null)
    {
      var r = await local.InvokeAsync(request).ConfigureAwait(false);
      return (TResponse)r!;
    }

    // No local handler found or timeout, avoids sitting in a black hole
    // TODO: Consider creating a custom Exception type for this scenario (there's a timeout ex below)
    if (!_usingIpcTransport && timeout is null)
      throw new TimeoutException("No IPC transport configured for request/response, and no local handler found.");

    if (_usingIpcTransport && _ipcEnvelopeTransport is null)
      throw new InvalidOperationException("No IPC transport configured for request/response.");

    // Edge case: Timeout response not tested with non-recepted transport
    // Don't allow it for now
    if (_ipcTransport is not null)
      throw new InvalidOperationException("Non-receipted IPC transports are allowed for request/response.");

    var correlationId = Guid.NewGuid().ToString("N");
    var pending = new PendingRequest { ResponseType = typeof(TResponse) };
    _pendingRequests[correlationId] = pending;

    EventEnvelope? envelope = null;
    if (_usingIpcTransport && _ipcEnvelopeTransport is not null)
    {
      envelope = EventSerializer.Wrap(
        request,
        isRequest: true,
        replyTo: _ipcEnvelopeTransport!.ReplyAddress,
        correlationId);
    }

    var cts = timeout.HasValue
      ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken)
      : null;

    // We know timeout has value here (!)
    cts?.CancelAfter(timeout!.Value);

    var effectiveCt = cts?.Token ?? cancellationToken;

    using (effectiveCt.Register(() =>
    {
      if (_pendingRequests.TryRemove(correlationId, out var pr))
      {
        // TODO (2025-11-28): Fix receipted timeout exception handling. It always throws and fails to cast to TResponse
        //// pr.Payload.SetResult(pr);
        pr.Payload.TrySetException(new TimeoutException($"Request timed out after {timeout ?? _defaultTimeout}."));
      }
    }))
    {
      // For receipted IPC events only
      if (envelope is not null)
        await _ipcEnvelopeTransport!.SendAsync(envelope, effectiveCt).ConfigureAwait(false);

      // TODO: (2025-11-28): Fix receipted timeout exception handling. It always fails to cast 'PendingRequest' to TResponse
      var obj = await pending.Payload.Task.ConfigureAwait(false);
      return (TResponse)obj!;
    }
  }

  /// <inheritdoc/>
  public void Subscribe<TEvent>(Action<TEvent> handler)
  {
    var list = _eventHandlers.GetOrAdd(
      typeof(TEvent),
      _ => new List<IWeakAction>());

    list.Add(new WeakAction<TEvent>(handler));

    /*
    var eventType = typeof(TEvent);
    var weakHandler = new WeakReference(handler);

    _eventSubscribers.AddOrUpdate(eventType,
      _ => new List<WeakReference> { weakHandler },
      (_, handlers) =>
    {
      handlers.Add(weakHandler);
      return handlers;
    });
    */
  }

  /// <inheritdoc/>
  public void SubscribeRequest<TRequest, TResponse>(Func<TRequest, Task<TResponse>> handler)
  {
    var handlers = _requestHandlers.GetOrAdd(typeof(TRequest), _ => new List<IRequestHandler>());
    handlers.Add(new RequestHandler<TRequest, TResponse>(handler));

    /*
    var wr = new WeakReference(handler);
    _requestSubscribers.AddOrUpdate(typeof(TRequest), _ => [wr], (_, list) =>
    {
      list.Add(wr);
      return list;
    });
    */
  }

  /// <inheritdoc/>
  public void Unsubscribe<TEvent>(Action<TEvent> handler)
  {
    if (_eventHandlers.TryGetValue(typeof(TEvent), out var handlers))
    {
      for (int i = handlers.Count - 1; i >= 0; i--)
      {
        var h = handlers[i];
        if (!h.IsAlive || h.Matches(handler))
          handlers.RemoveAt(i);
      }
    }

    /*
    if (_eventSubscribers.TryGetValue(typeof(TEvent), out var handlers))
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
    */
  }

  /// <inheritdoc/>
  public void UnsubscribeRequest<TRequest, TResponse>(Func<TRequest, Task<TResponse>> handler)
  {
    if (_requestHandlers.TryGetValue(typeof(TRequest), out var list))
    {
      for (int i = list.Count - 1; i >= 0; i--)
      {
        var h = list[i];
        if (!h.IsAlive || h.Matches(handler))
          list.RemoveAt(i);
      }
    }

    /*
    if (_requestSubscribers.TryGetValue(typeof(TRequest), out var handlers))
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
    */
  }

  /// <inheritdoc/>
  public async Task UseIpcEnvelopeTransportAsync(IEventEnvelopeTransport transport, CancellationToken cancellationToken = default)
  {
    if (_ipcTransport is not null)
      _ipcTransport = null;

    _usingIpcTransport = true;
    _ipcEnvelopeTransport = transport;
    await _ipcEnvelopeTransport.StartAsync(OnTransportMessageAsync, cancellationToken);
  }

  /// <inheritdoc/>
  public void UseIpcTransport(IEventTransport transport)
  {
    if (_ipcEnvelopeTransport is not null)
      _ipcEnvelopeTransport = null;

    _usingIpcTransport = true;
    _ipcTransport = transport;
  }

  /// <summary>Dispatches a local event with the specified payload of type <typeparamref name="T"/>.</summary>
  /// <typeparam name="T">The type of the event payload to be dispatched.</typeparam>
  /// <param name="payload">The payload data to include with the dispatched event.</param>
  private void DeliverLocalGeneric<T>(T payload) => DispatchEventLocal(payload);

  private void DispatchEventLocal<TEvent>(TEvent eventData)
  {
    // Iterate through subscribers (aka: handlers) and invoke
    if (_eventHandlers.TryGetValue(typeof(TEvent), out var subs))
    {
      for (int i = subs.Count - 1; i >= 0; i--)
      {
        var h = subs[i];
        if (!h.IsAlive)
        {
          subs.RemoveAt(i);
          continue;
        }

        h.InvokeObject(eventData!);
      }
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
  private IRequestHandler? GetFirstRequestHandler(Type requestType)
  {
    if (_requestHandlers.TryGetValue(requestType, out var handlers))
    {
      for (int i = handlers.Count - 1; i >= 0; i--)
      {
        var h = handlers[i];
        if (!h.IsAlive)
        {
          handlers.RemoveAt(i);
          continue;
        }

        return h;
      }
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
    var eventType = Type.GetType(envelope.EventType, throwOnError: false);
    if (eventType is null)
      return;

    if (envelope.IsResponse)
    {
      if (_pendingRequests.TryRemove(envelope.CorrelationId, out var pr))
      {
        try
        {
          var obj = JsonSerializer.Deserialize(envelope.PayloadJson, pr.ResponseType);
          pr.Payload.TrySetResult(obj);
        }
        catch (Exception ex)
        {
          pr.Payload.TrySetException(ex);
        }
      }

      return;
    }

    var payloadObj = JsonSerializer.Deserialize(envelope.PayloadJson, eventType);
    if (payloadObj is null)
      return;

    if (envelope.IsRequest)
    {
      var handler = GetFirstRequestHandler(eventType);
      if (handler is null)
        return;

      var responseObj = await handler.InvokeAsync(payloadObj).ConfigureAwait(false);

      if (responseObj is not null &&
          _ipcEnvelopeTransport is not null &&
          envelope.ReplyTo is not null)
      {
        var responseEnvelope = new EventEnvelope
        {
          MessageId = Guid.NewGuid().ToString("N"),
          CorrelationId = envelope.CorrelationId,
          EventType = responseObj.GetType().AssemblyQualifiedName!,
          IsRequest = false,
          IsResponse = true,
          ReplyTo = envelope.ReplyTo, // used by transport to route back to sender
          Timestamp = DateTimeOffset.UtcNow,
          PayloadJson = EventSerializer.Serialize(responseObj),
        };

        await _ipcEnvelopeTransport.SendAsync(responseEnvelope).ConfigureAwait(false);
      }

      return;
    }

    // One-way publish from transport:
    if (_eventHandlers.TryGetValue(eventType, out var list))
    {
      for (int i = list.Count - 1; i >= 0; i--)
      {
        var h = list[i];
        if (!h.IsAlive)
        {
          list.RemoveAt(i);
          continue;
        }

        h.InvokeObject(payloadObj);
      }
    }
  }

  private sealed class PendingRequest
  {
    /// <summary>Datatype of the response.</summary>
    public Type ResponseType { get; init; } = default!;

    /// <summary>Response object.</summary>
    public TaskCompletionSource<object?> Payload { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
  }
}
