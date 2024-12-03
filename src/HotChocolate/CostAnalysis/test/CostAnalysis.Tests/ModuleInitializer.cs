using System.Runtime.CompilerServices;

namespace HotChocolate.CostAnalysis;

internal static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Initialize()
    {
        CookieCrumbleXunit.Initialize();
        CookieCrumbleHotChocolate.Initialize();
    }
}
