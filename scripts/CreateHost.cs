using Microsoft.NET.HostModel.AppHost;
internal static class CreateHost
{
    private static void Main(string[] args) => HostWriter.CreateAppHost(args[0],args[1],"SlipShot.dll",true,args[2]);
}
