// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;

namespace SampleApp;

public class AspNetDISample
{
  /*
  using System.Net;
  using Lite.EventAggregator;
  using Microsoft.AspNetCore.Builder;
  using Microsoft.Extensions.DependencyInjection;

  var builder = WebApplication.CreateBuilder(args);
  builder.Services.AddEventAggregator();

  // Pick ONE transport:
  builder.Services.AddSingleton<IEventTransport>(sp =>
    new NamedPipeTransport("server-requests-in","client-requests-in","server-replies-in"));

  // or MemoryMappedTransport / TcpTransport (see earlier)
  builder.Services.AddHostedService<EventAggregatorTransportHostedService>();

  var app = builder.Build();

  var agg = app.Services.GetRequiredService<IEventAggregator>();
  agg.SubscribeRequest<Ping, Pong>(req => Task.FromResult(new Pong(req.Message + " from ASP.NET")));

  app.MapGet("/request/{msg}", async (string msg) =>
  {
    var resp = await agg.RequestAsync<Ping, Pong>(new Ping(msg));
    return resp.Message;
  });

  app.Run();

  public record Ping(string Message);
  public record Pong(string Message);

   */
}
