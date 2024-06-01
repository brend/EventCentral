namespace Waldenware.Events;

public interface IUnsubscriber: IDisposable
{
    void Unsubscribe();
}