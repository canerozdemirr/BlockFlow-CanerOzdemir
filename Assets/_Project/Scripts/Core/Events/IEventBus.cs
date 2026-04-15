using System;

// Subscribe returns an IDisposable — disposing it unsubscribes.
public interface IEventBus
{
    void Publish<T>(T evt);
    IDisposable Subscribe<T>(Action<T> handler);
}
