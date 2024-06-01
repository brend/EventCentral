# EventCentral
An event dispatch mechanism

## Add using NuGet
```sh
dotnet package add EventCentral
```

## Usage examples
Obtain a reference to the default EventCentral instance and subscribe
an observer for simple string notifications.

```C#
var eventCentral = EventCentral.Default;
var unsubscriber = eventCentral.Subscribe((string message) =>
  Console.WriteLine($"This just in: {message}"));
```

Then publish a notification from anywhere in your application.

```C#
eventCentral.Publish("You rock!"); // this will print "This just in: You rock!"
```

The unsubscriber returned by a call to `Subscribe` can be used to remove
the subscription.

```C#
unsubscriber.Unsubscribe();
eventCentral.Publish("Do you still rock?"); // this event won't be handled anymore
```

Unsubscribers implement `IDisposable` and will unsubscribe when they are
disposed of.

Events are by default identified by their type name, but it is also possible 
to specify event names explicitly.

```C#
struct MouseEvent
{
  public float X, Y;
  public int Button;
}
void mouseEventHandler(MouseEvent e) { /* do something with the event */ };
void rightClickEventHandler(MouseEvent e) { /* do something with the event */ };

// subscribe to mouse events by the name of "MouseEvent"
eventCentral.Subscribe(mouseEventHandler);

// subscribe to mouse events by the name of "RightClickEvent"
eventCentral.Subscribe(rightClickEventHandler, eventName: "RightClickEvent");

// publish some events
eventCentral.Publish(new MouseEvent { X = 17, Y = 4, Button = 1 }); // this will be handled only by mouseEventHandler
eventCentral.Publish(new MouseEvent { X = 32, Y = 8, Button = 2},
  eventName: "RightClickEvent"); // this will be handled only by rightClickEventHandler
```
