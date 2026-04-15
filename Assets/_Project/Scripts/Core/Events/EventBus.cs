using System;
using System.Collections.Generic;

public sealed class EventBus : IEventBus
{
    private readonly Dictionary<Type, Delegate> handlers = new Dictionary<Type, Delegate>();

    public void Publish<T>(T evt)
    {
        if (handlers.TryGetValue(typeof(T), out Delegate existing))
            (existing as Action<T>)?.Invoke(evt);
    }

    public IDisposable Subscribe<T>(Action<T> handler)
    {
        if (handler == null) throw new ArgumentNullException(nameof(handler));

        Type key = typeof(T);
        handlers.TryGetValue(key, out Delegate existing);
        handlers[key] = (existing as Action<T>) + handler;
        return new Subscription<T>(this, handler);
    }

    private void UnsubscribeInternal<T>(Action<T> handler)
    {
        Type key = typeof(T);
        if (!handlers.TryGetValue(key, out Delegate existing)) return;

        Action<T> updated = (existing as Action<T>) - handler;
        if (updated == null) handlers.Remove(key);
        else                 handlers[key] = updated;
    }

    // Safe to dispose more than once.
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
