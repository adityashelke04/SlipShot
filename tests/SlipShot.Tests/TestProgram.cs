using System;
using NUnitLite;
namespace SlipShot.Tests;
internal static class TestProgram
{
    [STAThread]
    public static int Main(string[] args) => new AutoRun().Execute(args);
}
