using System.Runtime.CompilerServices;

namespace HotChocolate.Data.Raven;

internal static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Initialize()
    {
        CookieCrumbleXunit.Initialize();
    }
}
