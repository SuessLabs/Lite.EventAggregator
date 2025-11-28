// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

namespace Lite.EventIpc;

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

// Helpers for JSON payload and envelope
public static class EventSerializer
{
  private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
  {
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    WriteIndented = false,
    Converters = { new JsonStringEnumConverter() },
  };

  public static T Deserialize<T>(string json) => JsonSerializer.Deserialize<T>(json, Options)!;

  public static string Serialize<T>(T obj) => JsonSerializer.Serialize(obj, Options);

  public static EventEnvelope Wrap<T>(T payload, bool isRequest, string? replyTo, string? correlationId = null)
  {
    return new EventEnvelope
    {
      MessageId = Guid.NewGuid().ToString("N"),
      CorrelationId = correlationId ?? Guid.NewGuid().ToString("N"),
      EventType = typeof(T).AssemblyQualifiedName!,
      IsRequest = isRequest,
      IsResponse = !isRequest && replyTo is null ? false : false, // set by sender when replying
      ReplyTo = replyTo,
      PayloadJson = Serialize(payload),
      Timestamp = DateTimeOffset.UtcNow,
    };
  }
}
