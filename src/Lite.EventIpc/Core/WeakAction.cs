// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System;

namespace Lite.EventIpc.Core;

/// <summary>Stores a weak reference to the delegate, and a typed invoker.</summary>
internal sealed class WeakAction<T> : IWeakAction
{
  private readonly WeakReference _delegateRef;

  public WeakAction(Action<T> handler)
  {
    _delegateRef = new WeakReference(handler);
  }

  public Type EventType => typeof(T);

  public bool IsAlive => _delegateRef.Target is Action<T>;

  public void InvokeObject(object payload)
  {
    var t = _delegateRef.Target as Action<T>;
    if (t != null && payload is T typed)
    {
      t(typed);
    }
  }

  public bool Matches(Delegate handler) => _delegateRef.Target is Action<T> a && a == (Action<T>)handler;
}
