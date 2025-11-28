// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Lite.EventAggregator.Tests;

[TestClass]
public class BaseTestClass
{
  /// <summary>Default timeout.. don't wait too long.</summary>
  public const int DefaultTimeout = 50;

  public const int DefaultTimeout200 = 200;

  public TestContext TestContext { get; set; }
}
