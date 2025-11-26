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
