using System.Runtime.CompilerServices;

namespace HotChocolate.Authorization;

internal static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Initialize()
    {
        CookieCrumbleXunit.Initialize();
        CookieCrumbleHotChocolate.Initialize();
    }
}
