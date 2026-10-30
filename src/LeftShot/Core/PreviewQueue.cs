using System;
using System.Collections.Generic;

namespace LeftShot.Core;

public sealed class PreviewQueue<T> where T : class
{
    private readonly Queue<T> pending = new();
    public T? Current { get; private set; }
    public int PendingCount => pending.Count;
    public IEnumerable<T> Items
    {
        get
        {
            if (Current is not null) yield return Current;
            foreach (T item in pending) yield return item;
        }
    }

    public void Push(T item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (Current is null) { Current = item; return; }
        if (pending.Count == 3) pending.Dequeue();
        pending.Enqueue(item);
    }

    public void Dismiss() => Current = pending.Count > 0 ? pending.Dequeue() : null;

    public void Clear()
    {
        Current = null;
        pending.Clear();
    }
}
