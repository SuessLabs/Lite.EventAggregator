# Architecture

In C#, an _event messenger_ typically refers to an implementation of the **Event Aggregator** or **Mediator** pattern, which facilitates communication between loosely coupled components without direct references. While C# has built-in events, a "messenger" provides a more centralized and decoupled way to publish and subscribe to messages.

## Core Concepts

* **Publisher (Sender)**: An object that sends a message. It doesn't need to know who the subscribers are.
* **Subscriber (Recipient)**: An object that receives and acts upon a message. It registers with the messenger to receive specific message types.
* **Messenger (Event Aggregator/Mediator)**: A central component that handles the registration of subscribers and the delivery of messages from publishers to the appropriate subscribers.

## Implementation Principles

* **Message Types**: Messages are typically defined as simple classes or structs, carrying the data relevant to the event.
* **Registration**: Subscribers register with the messenger, specifying the type of message they are interested in and providing a callback method (often an `Action<TMessage>`) to be invoked when that message type is published.
* **Sending/Broadcasting**: Publishers send messages to the messenger, which then dispatches them to all registered subscribers for that message type.
* **Decoupling**: The key advantage is that publishers and subscribers don't directly reference each other, reducing coupling and improving maintainability.

## Benefits

* **Loose Coupling**: Components are not tightly bound, making the system more flexible and easier to modify.
* **Improved Testability**: Individual components can be tested in isolation without needing to set up complex dependencies.
* **Centralized Communication**: Provides a clear and consistent way for different parts of an application to communicate.
* **Reduced Complexity**: Can simplify event handling in large applications by abstracting away direct event subscriptions.

## Considerations

* **Debugging**: Tracing message flow can sometimes be more challenging than with direct event subscriptions.
* **Overuse**: Should be used judiciously, as unnecessary indirection can add complexity.