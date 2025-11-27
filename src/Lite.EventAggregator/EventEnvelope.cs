// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System;

namespace Lite.EventAggregator;

/// <summary>Event Envelope.</summary>
public sealed class EventEnvelope
{
  public string CorrelationId { get; set; } = Guid.NewGuid().ToString("N");

  public string EventType { get; set; } = default!;

  public bool IsRequest { get; set; }

  public bool IsResponse { get; set; }

  public string MessageId { get; set; } = Guid.NewGuid().ToString("N");

  public string PayloadJson { get; set; } = default!;

  /// <summary>Gets or sets the transport-specific reply address (pipe name, host:port, etc.).</summary>
  public string? ReplyTo { get; set; }

  public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}
