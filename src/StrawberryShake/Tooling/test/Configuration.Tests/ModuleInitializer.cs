using System.Runtime.CompilerServices;

namespace StrawberryShake.Tools.Configuration;

internal static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Initialize()
    {
        CookieCrumbleXunit.Initialize();
    }
}
