using System;
using System.Collections.Generic;

/// <summary>
/// Minimal multicast-delegate-backed <see cref="IEventBus"/> implementation.
/// One <see cref="Delegate"/> per event <see cref="Type"/> in a dictionary;
/// <see cref="Publish{T}"/> resolves the entry and invokes it. Subscribers
/// hold a typed subscription token that unsubscribes on <see cref="IDisposable.Dispose"/>.
///
/// Keeping it this small is intentional: the case study doesn't need queued
/// events, priorities, or cross-thread dispatch. If any of those land later,
/// this is the single file that has to change.
/// </summary>
public sealed class EventBus : IEventBus
{
    private readonly Dictionary<Type, Delegate> handlers = new Dictionary<Type, Delegate>();

    public void Publish<T>(T evt)
    {
        if (handlers.TryGetValue(typeof(T), out var existing))
            (existing as Action<T>)?.Invoke(evt);
    }

    public IDisposable Subscribe<T>(Action<T> handler)
    {
        if (handler == null) throw new ArgumentNullException(nameof(handler));

        var key = typeof(T);
        handlers.TryGetValue(key, out var existing);
        handlers[key] = (existing as Action<T>) + handler;
        return new Subscription<T>(this, handler);
    }

    private void UnsubscribeInternal<T>(Action<T> handler)
    {
        var key = typeof(T);
        if (!handlers.TryGetValue(key, out var existing)) return;

        var updated = (existing as Action<T>) - handler;
        if (updated == null) handlers.Remove(key);
        else                 handlers[key] = updated;
    }

    /// <summary>
    /// Token returned from <see cref="Subscribe{T}"/>. Disposing it removes
    /// the handler from the bus. Safe to dispose more than once.
    /// </summary>
    private sealed class Subscription<T> : IDisposable
    {
        private EventBus bus;
        private Action<T> handler;

        public Subscription(EventBus bus, Action<T> handler)
        {
            this.bus = bus;
            this.handler = handler;
        }

        public void Dispose()
        {
            if (bus == null || handler == null) return;
            bus.UnsubscribeInternal(handler);
            bus = null;
            handler = null;
        }
    }
}
