using System.Runtime.CompilerServices;

namespace StrawberryShake.Transport.WebSockets;

internal static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Initialize()
    {
        CookieCrumbleXunit.Initialize();
    }
}
