// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System;

namespace Lite.EventIpc.Core;

/// <summary>
///   Defines a contract for actions that reference event handlers using weak references, enabling invocation and lifetime
///   checks without preventing garbage collection.
/// </summary>
/// <remarks>
///   Requirement: Internal Strongly-Typed Wrappers (No Reflection / No LINQ)
///   Implementations of this interface allow event handlers to be invoked while avoiding strong references
///   that could lead to memory leaks. This is typically used in event systems to ensure that subscribers can be collected
///   when no longer in use.
/// </remarks>
internal interface IWeakAction
{
  Type EventType { get; }

  bool IsAlive { get; }

  void InvokeObject(object payload);

  bool Matches(Delegate handler);
}
