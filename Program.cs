using Events;

var eventCentral = EventCentral.Instance;

eventCentral.Subscribe<string>(s => Console.WriteLine($"String event: {s}"));

eventCentral.Publish("Hello, world!");
