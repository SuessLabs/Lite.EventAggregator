// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;

namespace Lite.EventIpc.Core;

internal interface IRequestHandler
{
  bool IsAlive { get; }

  Type RequestType { get; }

  Type ResponseType { get; }

  Task<object?> InvokeAsync(object request);

  bool Matches(Delegate handler);
}
