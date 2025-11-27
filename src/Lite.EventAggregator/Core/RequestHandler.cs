// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;

namespace Lite.EventAggregator.Core;

/// <summary>
///   Provides a handler for asynchronous requests and responses, associating a delegate with specific request and
///   response types.
/// </summary>
/// <remarks>
///   This class enables dynamic invocation of request handlers by storing a delegate with weak reference
///   semantics, allowing for flexible and memory-efficient management of handler lifetimes. It is typically used in
///   scenarios where request and response types are determined at runtime, such as in generic messaging or command
///   frameworks.
/// </remarks>
/// <typeparam name="TRequest">The type of the request object that the handler processes.</typeparam>
/// <typeparam name="TResponse">The type of the response object returned by the handler.</typeparam>
internal sealed class RequestHandler<TRequest, TResponse> : IRequestHandler
{
  private readonly WeakReference _delegateRef;

  public RequestHandler(Func<TRequest, Task<TResponse>> handler)
  {
    _delegateRef = new WeakReference(handler);
  }

  public bool IsAlive => _delegateRef.Target is Func<TRequest, Task<TResponse>>;

  public Type RequestType => typeof(TRequest);

  public Type ResponseType => typeof(TResponse);

  public async Task<object?> InvokeAsync(object request)
  {
    if (_delegateRef.Target is not Func<TRequest, Task<TResponse>> delegObj)
      return null;

    if (request is not TRequest typed)
      return null;

    var result = await delegObj(typed).ConfigureAwait(false);
    return result!;
  }

  public bool Matches(Delegate handler) =>
    _delegateRef.Target is
      Func<TRequest, Task<TResponse>> h &&
      h == (Func<TRequest, Task<TResponse>>)handler;
}
