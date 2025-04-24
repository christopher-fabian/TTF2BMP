//-----------------------------------------------------------------------------
// Program.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Windows.Forms;

namespace TrueTypeConverter;

internal static class Program
{
  [STAThread]
  static void Main()
  {
    ApplicationConfiguration.Initialize();
    Application.Run(new MainForm());
  }
}