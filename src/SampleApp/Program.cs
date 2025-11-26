// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using Lite.EventAggregator;
using Lite.EventAggregator.Transporter;

namespace SampleApp;

public class Program
{
  public static void Main(string[] args)
  {
    /*
    builder.Services.AddSingleton<IEventAggregator, EventAggregator>();

    var aggregator = app.Services.GetRequiredService<IEventAggregator>();
    var transport = app.Services.GetRequiredService<IEventTransport>();
    */

    // IPC Transorter Examples
    var aggregator = new EventAggregator();

    // Enable Named Pipe IPC
    aggregator.EnableIpc(new NamedPipeTransport("MyPipe"));

    // Enable Memory-Mapped IPC
    // aggregator.EnableIpc(new MemoryMappedTransport("MySharedMemory"));

    // Enable TCP/IP IPC
    // aggregator.EnableIpc(new TcpTransport("127.0.0.1", 5000));
  }
}
