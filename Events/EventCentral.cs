namespace Waldenware.Events;

using EventName = string;

public sealed class EventCentral
{
    private static readonly Lazy<EventCentral> _defaultInstance = new(() => new EventCentral());
    public static EventCentral Default => _defaultInstance.Value;
    private readonly object _lock = new object();
    public EventCentral() { }

    internal readonly Dictionary<EventName, List<Subscription>> _subscribers = 
        new Dictionary<EventName, List<Subscription>>();

    public Action<Delegate> RunOnUiThread { get; set; } = _ => throw new InvalidOperationException("RunOnUiThread action not set");

    public IUnsubscriber Subscribe<TEvent>(Action<TEvent> handler, EventName? eventName = null, SubscriptionOptions options = SubscriptionOptions.None)
    {
        eventName ??= typeof(TEvent).Name;
        return SubscribeInternal(eventName, typeof(TEvent), handler, options);
    }

    private IUnsubscriber SubscribeInternal(
        string eventName, 
        Type eventType, 
        Delegate handler, 
        SubscriptionOptions options)
    {
        lock (_lock)
        {
            if (!_subscribers.TryGetValue(eventName, out var handlers))
            {
                handlers = new List<Subscription>();
                _subscribers.Add(eventName, handlers);
            }

            var subscription = new Subscription(eventType, handler, options);
            handlers.Add(subscription);
            return new Unsubscriber(this, eventName, subscription.Id);
        }
    }

    internal void Unsubscribe(EventName eventName, Guid subscriptionId)
    {
        lock (_lock)
        {
            if (_subscribers.TryGetValue(eventName, out var handlers))
            {
                handlers.RemoveAll(x => x.Id == subscriptionId);
                if (handlers.Count == 0)
                {
                    _subscribers.Remove(eventName);
                }
            }
        }
    }

    public void UnsubscribeAll(EventName? eventName = null)
    {
        lock (_lock)
        {
            if (eventName != null)
            {
                _subscribers.Remove(eventName);
            }
            else 
            {
                _subscribers.Clear();
            }
        }
    }

    public void UnsubscribeAll<TEvent>() => UnsubscribeAll(typeof(TEvent).Name);

    public void Publish<TEvent>(TEvent @event, EventName? eventName = null)
    {
        if (@event == null) throw new ArgumentNullException(nameof(@event));

        eventName ??= typeof(TEvent).Name;

        List<Subscription> handlersToInvoke = new List<Subscription>();
        List<Subscription> expiredHandlers = new List<Subscription>();

        // Lock around the retrieval of handlers to ensure the collection is not modified during publication
        lock (_lock)
        {
            if (_subscribers.TryGetValue(eventName, out var handlers))
            {
                handlersToInvoke.AddRange(handlers);
            }
        }

        // Invoke handlers outside of the lock to prevent deadlocks and to allow handlers to run concurrently
        foreach (var subscription in handlersToInvoke)
        {
            if (subscription.Handler.IsAlive && subscription.Handler.Target is Delegate handler)
            {
                try
                {
                    if (subscription.Options.HasFlag(SubscriptionOptions.RunOnUiThread))
                    {
                        RunOnUiThread(() => InvokeHandler(handler, @event));
                    }
                    else
                    {
                        InvokeHandler(handler, @event);
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Error invoking event handler: {ex}");
                }
            }
            else 
            {
                expiredHandlers.Add(subscription);
            }
        }

        // Remove expired handlers
        if (expiredHandlers.Count > 0)
        {
            lock (_lock)
            {
                foreach (var expiredHandler in expiredHandlers)
                {
                    _subscribers[eventName].Remove(expiredHandler);
                }
            }
        }
    }

    private void InvokeHandler(Delegate handler, object eventArgs)
    {
        handler.DynamicInvoke(eventArgs);
    }
}