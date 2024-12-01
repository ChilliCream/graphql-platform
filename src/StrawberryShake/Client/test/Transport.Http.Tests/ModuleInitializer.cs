using System.Runtime.CompilerServices;

namespace StrawberryShake.Transport.Http;

internal static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Initialize()
    {
        CookieCrumbleXunit.Initialize();
    }
}
