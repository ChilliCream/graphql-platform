using System.Runtime.CompilerServices;

namespace HotChocolate.Diagnostics;

internal static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Initialize()
    {
        CookieCrumbleXunit.Initialize();
    }
}
