using System;

/// <summary>
/// Typed in-process message bus. Publishers emit value-typed events;
/// subscribers receive them without knowing who sent them. The entire
/// Phase 6 gameplay loop communicates through this interface so no two
/// systems need a direct reference to one another.
///
/// <para>
/// <see cref="Subscribe{T}"/> returns an <see cref="IDisposable"/> — disposing
/// it unsubscribes. This avoids the usual "matching Unsubscribe call" pitfall
/// and plays nicely with service Dispose pipelines that already hold a list
/// of disposables.
/// </para>
/// </summary>
public interface IEventBus
{
    void Publish<T>(T evt);
    IDisposable Subscribe<T>(Action<T> handler);
}
