using System.Runtime.CompilerServices;

namespace HotChocolate.Data.Projections;

internal static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Initialize()
    {
        CookieCrumbleXunit.Initialize();
    }
}
