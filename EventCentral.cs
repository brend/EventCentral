namespace Events;

using EventName = System.String;

[Flags]
public enum SubscriptionOptions 
{
    None = 0,
    Async = 1,
    RunOnUiThread = 2,
}

class Subscription
{
    public Type EventType { get; }
    public Delegate Handler { get; }
    public SubscriptionOptions Options { get; }

    public Subscription(Type eventType, Delegate handler, SubscriptionOptions options)
    {
        EventType = eventType;
        Handler = handler;
        Options = options;
    }
}

public sealed class EventCentral
{
    private static readonly Lazy<EventCentral> _instance = new(() => new EventCentral());
    public static EventCentral Instance => _instance.Value;
    private EventCentral() { }

    private readonly Dictionary<EventName, List<Subscription>> _subscribers = 
        new Dictionary<EventName, List<Subscription>>();

    public void Subscribe<TEvent>(Action<TEvent> handler, SubscriptionOptions options = SubscriptionOptions.None)
    {
        var eventName = typeof(TEvent).Name;

        if (!_subscribers.TryGetValue(eventName, out var handlers))
        {
            handlers = new List<Subscription>();
            _subscribers.Add(eventName, handlers);
        }

        handlers.Add(new Subscription(typeof(TEvent), handler, options));
    }

    public void SubscribeAsync<TEvent>(Func<TEvent, Task> handler, SubscriptionOptions options = SubscriptionOptions.Async)
    {
        var eventName = typeof(TEvent).Name;
        
        options |= SubscriptionOptions.Async;

        if (!_subscribers.TryGetValue(eventName, out var handlers))
        {
            handlers = new List<Subscription>();
            _subscribers.Add(eventName, handlers);
        }

        handlers.Add(new Subscription(typeof(TEvent), handler, options));
    }

    public void Publish<TEvent>(TEvent @event)
    {
        ArgumentNullException.ThrowIfNull(@event);

        var eventName = typeof(TEvent).Name;

        if (_subscribers.TryGetValue(eventName, out var handlers))
        {
            foreach (var subscription in handlers)
            {
                if (subscription.Options.HasFlag(SubscriptionOptions.Async))
                {
                    Task.Run(() => subscription.Handler.DynamicInvoke(@event));
                }
                else
                {
                    subscription.Handler.DynamicInvoke(@event);
                }
            }
        }
    }

    public async Task PublishAsync<TEvent>(TEvent @event)
    {
        ArgumentNullException.ThrowIfNull(@event);

        var eventName = typeof(TEvent).Name;

        if (_subscribers.TryGetValue(eventName, out var handlers))
        {
            foreach (var subscription in handlers)
            {
                if (subscription.Options.HasFlag(SubscriptionOptions.Async))
                {
                    await (Task)subscription.Handler.DynamicInvoke(@event)!;
                }
                else
                {
                    subscription.Handler.DynamicInvoke(@event);
                }
            }
        }
    }

    public void Unsubscribe<TEvent>(Action<TEvent> handler)
    {
        var eventName = typeof(TEvent).Name;

        if (_subscribers.TryGetValue(eventName, out var handlers))
        {
            handlers.RemoveAll(s => ((MulticastDelegate)s.Handler).Equals(handler));
        }
    }
}