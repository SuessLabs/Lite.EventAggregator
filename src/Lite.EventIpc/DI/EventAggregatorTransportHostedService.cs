// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.
#if PREVIEW

using System.Threading;
using System.Threading.Tasks;
using Lite.EventAggregator.IpcReceiptTransport;
using Lite.EventAggregator.IpcTransport;
using Microsoft.Extensions.Hosting;

namespace Lite.EventAggregator;

public sealed class EventAggregatorTransportHostedService : IHostedService
{
  private readonly IEventAggregator _aggregator;
  private readonly IEventEnvelopeTransport _ipcEnvelopeTransport;
  private readonly IEventTransport _ipcTransport;

  public EventAggregatorTransportHostedService(IEventAggregator aggregator, IEventEnvelopeTransport transport)
  {
    _aggregator = aggregator;
    _ipcEnvelopeTransport = transport;
  }

  public Task StartAsync(CancellationToken cancellationToken)
    => _aggregator.UseIpcEnvelopeTransportAsync(_ipcEnvelopeTransport, cancellationToken);

  public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

#endif
