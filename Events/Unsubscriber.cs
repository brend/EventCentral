namespace Waldenware.Events;

using EventName = string;

internal class Unsubscriber : IUnsubscriber
{
    private readonly EventCentral _eventCentral;
    private readonly string _eventName;
    private readonly Guid _subscriptionId;
    private bool _isDisposed;

    public Unsubscriber(EventCentral eventCentral, EventName eventName, Guid subscriptionId)
    {
        _eventCentral = eventCentral;
        _eventName = eventName;
        _subscriptionId = subscriptionId;
    }

    public void Unsubscribe()
    {
        _eventCentral.Unsubscribe(_eventName, _subscriptionId);
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;
        Unsubscribe();
    }
}