# Lite Event Aggregator and IPC Tranporter

Lite.EventIPC is a cross-platform local Event Aggregator and remote IPC service library for C#. The pattern is used for decoupling publishers and subscribers in a single or multiple applications. The library can be easily extended for custom IPC transports using the `IEventTransport` interface to suit your needs (_need I say, DBus?_)

The Event Aggregator service in C# pattern is useful for decoupling publishers and subscribers in an application.

## Features

This implementation is features:

* Simple and thread-safe
* Uses **weak references** to avoid memory leaks
* Cleans up dead references during `Publish`.
* Prevents memory leaks when subscribers are no longer needed.
* DI-friendly with extensions and hosted service
* Optional IPC transport mechanisms for inter-process communication (IPC) with **JSON serialization**:
  * Named Pipe Transport
  * Memory-Mapped File Transport (_Windows OS only_)
  * TCP/IP Transport
* 2 types of IPC communication:
  * One-way publish/subscribe (`IEventTransport`)
  * (_COMING SOON_) Bidirectional request/response with timeouts (`IEventEnvelopeTransport`)

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

### v1.1.0

* Option to include `Microsoft.Extensions.Logger` for deep-logging

## Future Considerations

* Send IPC only for specified event types.
* Possibly, filtering or priority-based dispatching.
* Refactor `IEventTransport` to`IIpcEvent` for clarity.
* Refactor `Transporter` namespace to `Ipc` or `IpcTransport` for clarity.
* Refactor `IEventEnvelopeTransport` under `IpcReceipted` namespace for clarity.
* Security: Named Pipes, MMF, and TCP should use proper ACLs / TLS / auth in production
