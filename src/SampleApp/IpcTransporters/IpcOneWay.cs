// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using Lite.EventIpc;
using Lite.EventIpc.IpcTransport;

namespace SampleApp.IpcTransporters;

public class IpcOneWay
{
  public void DiRegistration()
  {
    /*
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.AspNetCore.Builder;
    using System.Net;

    var builder = WebApplication.CreateBuilder(args);
    builder.Services.AddSingleton<IEventAggregator, EventAggregator>();

    // Example: Named Pipes (one-way, publish only)
    // send requests/publishes to server
    builder.Services.AddSingleton<IEventTransport>(sp =>
      new NamedPipeTransport(
        outgoingPipeName: "server-requests-in"));

    var app = builder.Build();
    var aggregator = app.Services.GetRequiredService<IEventAggregator>();
    var transport = app.Services.GetRequiredService<IEventTransport>();

    // One-way transport (publishes only)
    // IPC Transorter Examples (choose one):
    // ----------------------------------
    // 1) Enable Named Pipe IPC
    // aggregator.UseIpcTransport(new NamedPipeTransport("MyPipe"));
    //
    // 2) Enable Memory-Mapped IPC
    // aggregator.UseIpcTransport(new MemoryMappedTransport("MySharedMemory"));
    //
    // 3) Enable TCP/IP IPC
    // aggregator.UseIpcTransport(new TcpTransport("127.0.0.1", 5000));

    app.Run();
    */
  }

  public void PublishExample()
  {
  }
}
