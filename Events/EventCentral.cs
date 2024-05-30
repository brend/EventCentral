namespace Waldenware.Events;

using EventName = string;

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

        if (!_subscribers.TryGetValue(eventName, out var handlers))
        {
            return;
        }
        
        foreach (var subscription in handlers)
        {
            if (subscription.Options.HasFlag(SubscriptionOptions.Async) && subscription.Options.HasFlag(SubscriptionOptions.RunOnUiThread))
            {
                Task.Run(() => RunOnUiThread(() => subscription.Handler.DynamicInvoke(@event)));
            }
            else if (subscription.Options.HasFlag(SubscriptionOptions.Async))
            {
                Task.Run(() => subscription.Handler.DynamicInvoke(@event));
            }
            else if (subscription.Options.HasFlag(SubscriptionOptions.RunOnUiThread))
            {
                RunOnUiThread(() => subscription.Handler.DynamicInvoke(@event));
            }
            else
            {
                subscription.Handler.DynamicInvoke(@event);
            }
        }
    }

    public async Task PublishAsync<TEvent>(TEvent @event)
    {
        ArgumentNullException.ThrowIfNull(@event);

        var eventName = typeof(TEvent).Name;

        if (!_subscribers.TryGetValue(eventName, out var handlers))
        {
            return;
        }

        foreach (var subscription in handlers)
        {
            if (subscription.Options.HasFlag(SubscriptionOptions.Async) && subscription.Options.HasFlag(SubscriptionOptions.RunOnUiThread))
            {
                await RunOnUiThreadAsync((Task)subscription.Handler.DynamicInvoke(@event)!);
            }
            else if (subscription.Options.HasFlag(SubscriptionOptions.Async))
            {
                await (Task)subscription.Handler.DynamicInvoke(@event)!;
            }
            else if (subscription.Options.HasFlag(SubscriptionOptions.RunOnUiThread))
            {
                RunOnUiThread(() => subscription.Handler.DynamicInvoke(@event));
            }
            else
            {
                subscription.Handler.DynamicInvoke(@event);
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