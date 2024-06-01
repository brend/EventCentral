namespace Waldenware.Events;

internal class Subscription
{
    public Guid Id { get; } = Guid.NewGuid();
    public Type EventType { get; }
    public WeakReference Handler { get; }
    public SubscriptionOptions Options { get; }

    public Subscription(Type eventType, Delegate handler, SubscriptionOptions options)
    {
        EventType = eventType;
        Handler = new WeakReference(handler);
        Options = options;
    }
}