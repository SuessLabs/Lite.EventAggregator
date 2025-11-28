// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.
#if PREVIEW
using Lite.EventIpc.IpcTransport;
using Microsoft.Extensions.DependencyInjection;

namespace Lite.EventIpc.Extensions;

public static class EventAggregatorServiceCollectionExtensions
{
  public static IServiceCollection AddEventAggregator(this IServiceCollection services)
  {
    services.AddSingleton<IEventAggregator, EventAggregator>();
    return services;
  }

  public static IServiceCollection UseEventTransport<TTransport>(this IServiceCollection services)
      where TTransport : class, IEventTransport
  {
    services.AddSingleton<IEventTransport, TTransport>();
    services.AddHostedService<EventAggregatorTransportHostedService>();
    return services;
  }
}
#endif
