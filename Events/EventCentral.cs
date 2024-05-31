namespace Waldenware.Events;

using EventName = string;

[Flags]
public enum SubscriptionOptions 
{
    None = 0,
    RunOnUiThread = 1,
}

internal class Subscription
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

    internal readonly Dictionary<EventName, List<Subscription>> _subscribers = 
        new Dictionary<EventName, List<Subscription>>();

    public Action<Delegate> RunOnUiThread { get; set; } = _ => throw new InvalidOperationException("RunOnUiThread action not set");

    public void Subscribe<TEvent>(Action<TEvent> handler, EventName? eventName = null, SubscriptionOptions options = SubscriptionOptions.None)
    {
        eventName ??= typeof(TEvent).Name;
        SubscribeInternal(eventName, typeof(TEvent), handler, options);
    }

    private void SubscribeInternal(
        string eventName, 
        Type eventType, 
        Delegate handler, 
        SubscriptionOptions options)
    {
        if (!_subscribers.TryGetValue(eventName, out var handlers))
        {
            handlers = new List<Subscription>();
            _subscribers.Add(eventName, handlers);
        }

        handlers.Add(new Subscription(eventType, handler, options));
    }

    public void Unsubscribe<TEvent>(Action<TEvent> handler)
    {
        var eventName = typeof(TEvent).Name;

        if (_subscribers.TryGetValue(eventName, out var handlers))
        {
            handlers.RemoveAll(s => ((MulticastDelegate)s.Handler).Equals(handler));
            if (handlers.Count == 0)
            {
                _subscribers.Remove(eventName);
            }
        }
    }

    public void UnsubscribeAll()
    {
        _subscribers.Clear();
    }

    public void Publish<TEvent>(TEvent @event, EventName? eventName = null)
    {
        ArgumentNullException.ThrowIfNull(@event);

        eventName ??= typeof(TEvent).Name;

        if (!_subscribers.TryGetValue(eventName, out var handlers))
        {
            return;
        }
        
        foreach (var subscription in handlers)
        {
            if (subscription.Options.HasFlag(SubscriptionOptions.RunOnUiThread))
            {
                RunOnUiThread(() => subscription.Handler.DynamicInvoke(@event));
            }
            else
            {
                subscription.Handler.DynamicInvoke(@event);
            }
        }
    }
}