using Events;

var eventCentral = EventCentral.Instance;

eventCentral.RunOnUiThread = handler => 
{
    Console.WriteLine("Running on UI thread");
    handler.DynamicInvoke();
};

eventCentral.Subscribe<string>(s => Console.WriteLine($"String event: {s}"));
eventCentral.SubscribeAsync<string>(async s => 
{
    await Task.Delay(1000);
    Console.WriteLine($"Async string event: {s}");
}, SubscriptionOptions.RunOnUiThread);

await eventCentral.PublishAsync("Hello, world!");