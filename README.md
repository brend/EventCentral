# EventCentral
An event dispatch mechanism

## Usage examples
Obtain a reference to the default EventCentral instance and subscribe
an observer for simple string notifications.

```C#
var eventCentral = EventCentral.Instance;

eventCentral.Subscribe((string message) => Console.WriteLine($"This just in: {message}"));
```

Then publish a notification from anywhere in your application.

```C#
eventCentral.Publish("You rock!");
```

Async observers are supported.

```C#
eventCentral.Subscibe()
```