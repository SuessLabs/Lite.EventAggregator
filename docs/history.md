## History

### v1.1.0

* Option to include `Microsoft.Extensions.Logger` for deep-logging

### v1.0.0

* [New] Renamed library to "Lite.EventIpc"
  * Library includes local Event Aggregator and remote IPC transports
* [New] Removed relyance on reflection
* [New] Added ability for local event timeouts.
  * This can happen when there are no subscribers, but you expect there to be one. Previously this was only available for "receipted" IPC transports.
* [vNext] Receipted IPC Transports bumped to next version (_`IEventEnvelopeTransport` still exists, just not implemented_)
  * IPC Transport timeouts for 'Envelope' (receipted) messages

### v0.9.0

* Async publish + request/response
* **Strongly-typed** internal wrappers (no reflection & no System.Linq)
* **Timeout support** in RequestAsync
* **Three IPC transports** _(Named Pipes, Memory-Mapped Files, TCP/IP)_
* DI extensions + hosted service
* **Unit tests**
* **Full working demos** (one per transport)

* No reflection/linq path in handler dispatch�strongly-typed wrappers are used.
* Weak references to delegates to avoid leaks.
* Timeout support via RequestAsync parameter (default 5s).
* Length-prefixed framing across pipe/tcp; EventWaitHandle for MMF.
* DI-friendly + hosted service to start transport automatically.
* Unit tests for local, timeout, and each transport.
* Portable across .NET 7/8.

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
  * `TcpTransport` _(two ports�one for requests, one for responses�with length-prefix framing)_

#### Future Improvements

* **Reliability:** Durable queues, consider MSMQ/Service Bus/RabbitMQ/Kafka depending on needs. (Out of scope for this lightweight first-pass.)
* **Security:** Named Pipes can use ACLs; TCP needs TLS + auth; MMFs need OS ACLs. Add validation and schema versioning (i.e., `EventEnvelope`).
* **Backpressure & Flow Control:** Implement bounded queues, retry, and exponential backoff where applicable.
* **Type Resolution:** `AssemblyQualifiedName` assumes shared assemblies across processes. Consider a type registry or message contracts package shared by both sides.
* **Framing:** Length-prefix framing prevents stream-boundary issues; keep consistent across transports.
* **Concurrency:** The Memory-Mapped example is single-slot; for multiple writers/readers, implement a ring buffer or per-message files + directory watcher.
* Add cancellation-aware timeouts and retry policies.
* Provide a full two-process demo (client & server) for each transport so you can run them separately and see request/response in action.

### v0.7.0

Adds optional (one-way) IPC transport mechanisms for inter-process communication (IPC) with **JSON serialization**. IPC can be integrated with the `IEventTransport` interface.

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
