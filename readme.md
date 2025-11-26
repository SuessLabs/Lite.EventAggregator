# Lite Event Aggregator in C#

The Event Aggregator service in C# pattern is useful for decoupling publishers and subscribers in an application.

## Features

This implementation is features:

* Simple and thread-safe
* Uses **weak references** to avoid memory leaks
* Cleans up dead references during `Publish`.
* Prevents memory leaks when subscribers are no longer needed.

## Usage

```cs

public class UserCreatedEvent
{
  public string UserName { get; set; }
}


static void Main()
{
  var eventAggregator = new EventAggregator();

  // Subscribe
  eventAggregator.Subscribe<UserCreatedEvent>(e =>
    Console.WriteLine($"User created: {e.Username}"));

  // Publish
  eventAggregator.Publish(new UserCreatedEvent { Username = "Damian" })
}
```

## Architecture

### Why Weak References

If you store strong references to handlers, subscribers will never be collected. Using `WeakReference` ensures that if the subscriber is no longer needed, it can be GC'd.

## History

### v0.8.0

Evolved the Event Aggregator to support **async**, **bidirectional** request/response, and **pluggable IPC transports** via Named Pipes, Memory-Mapped Files, and TCP/IP Sockets.
This design preserves your weak-reference handlers, remains DI-friendly, and keeps publishers decoupled from subscribers and transport mechanics.

> **Note:**
> 1. The implementation intentionally straightforward. Not for production at this time as it needs robust error handling, retries, backpressure, queueing, auth/ACLs, and schema versioning.
> 2. Consider bringing back `SendAsync<TEvent>(..)` and `StartAsync<TEvent>(..)` along side the new `IEventTransport` bi-directional sender/receivers.

* **`EventAggregator` (DI singleton):**
  * `PublishAsync<TEvent>(TEvent eventData)`
  * `RequestAsync<TRequest, TResponse>(TRequest request)`
  * `Subscribe<TEvent>(Action<TEvent> handler)` _(one-way)_
  * `SubscribeRequest<TRequest, TResponse>(Func<TRequest, Task<TResponse>> handler)` _(request/response)_
* **`IEventTransport` (async, bi-directional):**
  * `StartAsync(Func<EventEnvelope, Task> onMessageAsync, CancellationToken ct)`
  * `SendAsync(EventEnvelope envelope, CancellationToken ct)`
  * `ReplyAddress { get; }` _(used by aggregator to populate ReplyTo for requests)_
* **Transports:**
  * `NamedPipeTransport` _(duplex via named server/client pipes + length-prefix framing)_
  * `MemoryMappedTransport` _(two MMFs + named EventWaitHandles for request/response signals)_
  * `TcpTransport` _(two ports—one for requests, one for responses—with length-prefix framing)_

### v0.7.0

Adds optional IPC transport mechanisms for inter-process communication (IPC) with **JSON serialization**. IPC can be integrated with the `IEventTransport` interface.

* **Named Pipe** Transport
* **Memory-Mapped File** Transport (_Windows OS only_)
* **TCP/IP** Transport

### v0.6.0

* Uses **weak references** to avoid memory leaks
* Cleans up dead references during `Publish`.
* Prevents memory leaks when subscribers are no longer needed.

### v0.5.0

* Simple and thread-safe
* Added example unit test for usage

## Future Considerations

* **Async** support for event handling.
* Possibly, filtering or priority-based dispatching.
