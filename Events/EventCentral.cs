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

    private readonly Dictionary<EventName, List<Subscription>> _subscribers = 
        new Dictionary<EventName, List<Subscription>>();

    public Action<Delegate> RunOnUiThread { get; set; } = _ => throw new InvalidOperationException("RunOnUiThread action not set");

    private async Task RunOnUiThreadAsync(Task task)
    {
        var tcs = new TaskCompletionSource<bool>();

        RunOnUiThread(() =>
        {
            task.ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    tcs.SetException(t.Exception!.InnerException!);
                }
                else if (t.IsCanceled)
                {
                    tcs.SetCanceled();
                }
                else
                {
                    tcs.SetResult(true);
                }
            });
        });

        await tcs.Task;
    }

    public void Subscribe<TEvent>(Action<TEvent> handler, EventName? eventName = null, SubscriptionOptions options = SubscriptionOptions.None)
    {
        eventName ??= typeof(TEvent).Name;
        SubscribeInternal(eventName, typeof(TEvent), handler, options);
    }

    public void Subscribe<TEvent>(Func<TEvent, Task> handler, EventName? eventName, SubscriptionOptions options = SubscriptionOptions.None)
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
        }
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
            switch (subscription.Handler)
            {
                case Action<TEvent> action:
                    if (subscription.Options.HasFlag(SubscriptionOptions.RunOnUiThread))
                    {
                        RunOnUiThread(() => action(@event));
                    }
                    else
                    {
                        action(@event);
                    }
                    break;
                case Func<TEvent, Task> func:
                    if (subscription.Options.HasFlag(SubscriptionOptions.RunOnUiThread))
                    {
                        RunOnUiThreadAsync(func(@event)).Wait();
                    }
                    else
                    {
                        func(@event).Wait();
                    }
                    break;
            }

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

    public async Task PublishAsync<TEvent>(TEvent @event, EventName? eventName = null)
    {
        ArgumentNullException.ThrowIfNull(@event);

        eventName = typeof(TEvent).Name;

        if (!_subscribers.TryGetValue(eventName, out var handlers))
        {
            return;
        }

        foreach (var subscription in handlers)
        {
            switch (subscription.Handler)
            {
                case Action<TEvent> action:
                    if (subscription.Options.HasFlag(SubscriptionOptions.RunOnUiThread))
                    {
                        RunOnUiThread(() => action(@event));
                    }
                    else
                    {
                        action(@event);
                    }
                    break;
                case Func<TEvent, Task> func:
                    if (subscription.Options.HasFlag(SubscriptionOptions.RunOnUiThread))
                    {
                        await RunOnUiThreadAsync(func(@event));
                    }
                    else
                    {
                        await func(@event);
                    }
                    break;
            }
        }
    }
}