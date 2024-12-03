using System.Runtime.CompilerServices;

namespace HotChocolate.Data.Projections.Spatial;

internal static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Initialize()
    {
        CookieCrumbleXunit.Initialize();
    }
}
