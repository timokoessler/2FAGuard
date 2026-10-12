using System.Runtime.InteropServices;
using Guard.Core.Platform;

namespace Guard.Core.Platform;

public static class PlatformService
{
    private static IPlatformService? _instance;
    
    public static IPlatformService Instance => _instance ??= CreatePlatformService();
    
    public static void SetInstance(IPlatformService instance)
    {
        _instance = instance;
    }
    
    private static IPlatformService CreatePlatformService()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return new WindowsPlatformService();
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return new LinuxPlatformService();
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return new MacPlatformService();
        }
        
        return new FallbackPlatformService();
    }
}