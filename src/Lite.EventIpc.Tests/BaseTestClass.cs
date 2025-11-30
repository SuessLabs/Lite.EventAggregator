// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;

namespace Lite.EventIpc.Tests;

[TestClass]
public class BaseTestClass
{
  /// <summary>Default timeout.. don't wait too long.</summary>
  public const int DefaultTimeout = 50;

  public const int DefaultTimeout200 = 200;

  [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:Fields should be private", Justification = "This is a parent class")]
  protected ILogger<EventAggregator>? _logger;

  public TestContext TestContext { get; set; }

  protected ILogger<T> CreateConsoleLogger<T>(LogLevel minimumLevel = LogLevel.Debug)
  {
    var factory = LoggerFactory.Create(config =>
    {
      //// config.AddConsole();
      config.AddConsole(options =>
      {
        options.TimestampFormat = "[HH:mm:ss.fff] ";
        options.UseUtcTimestamp = false;
        options.IncludeScopes = true;
      });
      config.SetMinimumLevel(minimumLevel);
    });

    return factory.CreateLogger<T>();
  }
}
