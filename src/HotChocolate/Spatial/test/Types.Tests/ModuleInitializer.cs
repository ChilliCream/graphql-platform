using System.Runtime.CompilerServices;

namespace HotChocolate.Types.Spatial;

internal static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Initialize()
    {
        CookieCrumbleXunit.Initialize();
        CookieCrumbleHotChocolate.Initialize();
    }
}
