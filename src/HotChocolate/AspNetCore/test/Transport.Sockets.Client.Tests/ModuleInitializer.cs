using System.Runtime.CompilerServices;

namespace HotChocolate.Transport.Sockets;

internal static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Initialize()
    {
        CookieCrumbleXunit.Initialize();
    }
}
