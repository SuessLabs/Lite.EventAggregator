// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

namespace SampleApp.IpcTransporters;

public class IpcBidirectional
{
  public void DiRegistration()
  {
    /*
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.AspNetCore.Builder;
    using System.Net;

    // Program.cs
    var builder = WebApplication.CreateBuilder(args);

    // Choose ONE transport and register it + aggregator
    builder.Services.AddSingleton<IEventAggregator, EventAggregator>();

    // Example: Named Pipes (client-side perspective)
    builder.Services.AddSingleton<IEventTransport>(sp =>
      new NamedPipeTransport(
        incomingPipeName: "client-requests-in",  // this process listens (if needed)
        outgoingPipeName: "server-requests-in",  // send requests/publishes to server
        replyPipeName: "client-replies-in"));    // this process listens for responses

    // Example: Memory-Mapped Files
    // builder.Services.AddSingleton<IEventTransport>(sp =>
    //     new MemoryMappedTransport("req-map", "resp-map", "req-signal", "resp-signal"));

    // Example: TCP
    // builder.Services.AddSingleton<IEventTransport>(sp =>
    //   new TcpTransport(
    //     requestListen: new IPEndPoint(IPAddress.Loopback, 5001),
    //     replyListen:   new IPEndPoint(IPAddress.Loopback, 5002),
    //     requestSend:   new IPEndPoint(IPAddress.Loopback, 5000), // other side request port
    //     replySend:     new IPEndPoint(IPAddress.Loopback, 5003))); // other side reply port

    var app = builder.Build();

    var aggregator = app.Services.GetRequiredService<IEventAggregator>();
    var transport = app.Services.GetRequiredService<IEventTransport>();
    await aggregator.UseIpcEnvelopeTransportAsync(transport);

    // Sample subscriptions
    aggregator.Subscribe<UserCreatedEvent>(e => Console.WriteLine($"[Local] User created: {e.Username}"));

    aggregator.SubscribeRequest<GetUserRequest, GetUserResponse>(async req =>
    {
      await Task.Delay(25);
      return new GetUserResponse { Username = req.Username, Exists = true };
    });

    app.Run();

    // Sample event types
    public record UserCreatedEvent(string Username);
    public record GetUserRequest(string Username);
    public record GetUserResponse { public string Username { get; set; } = ""; public bool Exists { get; set; } }
    */
  }

  public void PublishingAndRequesting()
  {
    /*
    public class UserService
    {
      private readonly IEventAggregator _aggregator;

      public UserService(IEventAggregator aggregator) => _aggregator = aggregator;

      public async Task CreateAsync(string username, CancellationToken ct = default)
      {
        await _aggregator.PublishAsync(new UserCreatedEvent(username), ct);
      }

      public async Task<bool> CheckUserExistsAsync(string username, CancellationToken ct = default)
      {
        var resp = await _aggregator.RequestAsync<GetUserRequest, GetUserResponse>(new GetUserRequest(username), ct);
        return resp.Exists;
      }
    }
    */
  }
}
