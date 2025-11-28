// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System;
using Lite.EventAggregator;

namespace SampleApp;

public class Program
{
  public static void Main(string[] args)
  {
    var eventAggregator = new EventAggregator();

    // Subscribe
    eventAggregator.Subscribe<SampleEvent>(evt => Console.WriteLine(evt.Message));

    // Publish
    eventAggregator.Publish(new SampleEvent { Message = "Hello from, Lite.EventIpc's EventAggregator" });
  }

  public class SampleEvent
  {
    public string Message { get; set; }
  }
}
