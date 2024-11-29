using System.Runtime.CompilerServices;

namespace HotChocolate.Language;

internal static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Initialize()
    {
        CookieCrumbleXunit.Initialize();
    }
}
