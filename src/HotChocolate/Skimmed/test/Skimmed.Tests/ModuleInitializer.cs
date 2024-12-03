using System.Runtime.CompilerServices;

namespace HotChocolate.Skimmed;

internal static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Initialize()
    {
        CookieCrumbleXunit.Initialize();
    }
}
