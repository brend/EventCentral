using Events;

var eventCentral = EventCentral.Instance;

eventCentral.Subscribe<string>(s => Console.WriteLine($"String event: {s}"));
eventCentral.SubscribeAsync<string>(async s => 
{
    await Task.Delay(1000);
    Console.WriteLine($"Async string event: {s}");
});

await eventCentral.PublishAsync("Hello, world!");
