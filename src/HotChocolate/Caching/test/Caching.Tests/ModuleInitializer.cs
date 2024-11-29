using System.Runtime.CompilerServices;

namespace HotChocolate.Caching.Tests;

internal static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Initialize()
    {
        CookieCrumbleHotChocolate.Initialize();
    }
}
