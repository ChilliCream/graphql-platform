using System.Runtime.CompilerServices;

namespace StrawberryShake.Transport.InMemory;

internal static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Initialize()
    {
        CookieCrumbleXunit.Initialize();
    }
}
