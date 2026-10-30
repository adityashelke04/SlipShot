using System;
using System.Threading;
using System.Windows;
using SlipShot.Services;

namespace SlipShot;
internal static class Program
{
    [STAThread]
    public static int Main()
    {
        // Share the legacy mutex so an older build cannot capture concurrently.
        using var instance=new Mutex(true,@"Local\LeftShot.Portable.0.1",out bool created);
        if(!created) return 0;
        // Also needed for the SDK-less DLL entry point, whose host manifest belongs to dotnet.
        Native.SetProcessDpiAwarenessContext(new IntPtr(-4));
        var app=new Application {ShutdownMode=ShutdownMode.OnExplicitShutdown};
        using var controller=new AppController(app);
        app.Startup+=(_,_)=>controller.Start();
        return app.Run();
    }
}
