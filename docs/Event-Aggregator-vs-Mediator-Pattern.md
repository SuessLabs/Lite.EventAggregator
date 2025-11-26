# Event Aggregator vs Mediator Pattern

An Event Aggregator simply decouples publishers from subscribers by broadcasting events without knowledge of the recipients, often using a "fire and forget" model. A Mediator, however, is a more active pattern that encapsulates complex communication and can include logic to manage, control, and transform messages before they are sent to a known set of interacting objects. While an Event Aggregator can be considered a type of Mediator, the key difference is the presence of active control and business logic in the Mediator, whereas the Event Aggregator's sole purpose is to pass messages from publishers to subscribers.

## Event Aggregator

* **Purpose**: To reduce coupling between publishers and subscribers by providing a central hub for events.
* **Communication Style**: "Fire and forget." Publishers fire an event to the aggregator and move on, without knowing who the subscribers are or even if there are any.
* **Logic**: Typically contains little to no business logic. It simply redirects events from publishers to subscribers.
* **Use Case**: Ideal for UI synchronization and indirect communication between various components where you want to avoid direct dependencies between them.

## Mediator Pattern

* **Purpose**: To encapsulate how a set of objects interact, reducing direct dependencies between them.
* **Communication Style**: Objects communicate indirectly through the mediator, which actively manages the conversation.
* **Logic**: Includes logic to coordinate communication and can manipulate or filter messages before passing them along.
* **Use Case**: Used when there is complex interaction between a known set of objects, such as a workflow wizard, or when you need to enforce specific rules or coordinate the behavior of multiple components.
