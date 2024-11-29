using System.Runtime.CompilerServices;

namespace HotChocolate.Data.Spatial.Filters;

internal static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Initialize()
    {
        CookieCrumbleXunit.Initialize();
    }
}
