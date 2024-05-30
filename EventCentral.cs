namespace Events;

public sealed class EventCentral
{
    private static readonly Lazy<EventCentral> _instance = new(() => new EventCentral());
    public static EventCentral Instance => _instance.Value;
    private EventCentral() { }

    private readonly Dictionary<Type, List<object>> _registry = [];

    public void Subscribe<TEvent>(Action<TEvent> handler)
    {
        if (!_registry.TryGetValue(typeof(TEvent), out var handlers))
        {
            handlers = [];
            _registry.Add(typeof(TEvent), handlers);
        }

        handlers.Add(handler);
    }

    public void SubscribeAsync<TEvent>(Func<TEvent, Task> handler)
    {
        if (!_registry.TryGetValue(typeof(TEvent), out var handlers))
        {
            handlers = [];
            _registry.Add(typeof(TEvent), handlers);
        }

        handlers.Add(handler);
    }

    public void Publish<TEvent>(TEvent @event)
    {
        if (_registry.TryGetValue(typeof(TEvent), out var handlers))
        {
            foreach (var handler in handlers.OfType<Action<TEvent>>())
            {
                handler(@event);
            }

            foreach (var asyncHandler in handlers.OfType<Func<TEvent, Task>>())
            {
                asyncHandler(@event).ConfigureAwait(false);
            }
        }
    }

    public async Task PublishAsync<TEvent>(TEvent @event)
    {
        if (_registry.TryGetValue(typeof(TEvent), out var handlers))
        {
            var tasks = handlers.OfType<Func<TEvent, Task>>()
                                .Select(handler => handler(@event))
                                .ToList();

            foreach (var handler in handlers.OfType<Action<TEvent>>())
            {
                handler(@event);
            }

            await Task.WhenAll(tasks);
        }
    }

    public void Unsubscribe<TEvent>(Action<TEvent> handler)
    {
        if (_registry.TryGetValue(typeof(TEvent), out var handlers))
        {
            handlers.Remove(handler);
        }
    }

    public void UnsubscribeAsync<TEvent>(Func<TEvent, Task> handler)
    {
        if (_registry.TryGetValue(typeof(TEvent), out var handlers))
        {
            handlers.Remove(handler);
        }
    }
}