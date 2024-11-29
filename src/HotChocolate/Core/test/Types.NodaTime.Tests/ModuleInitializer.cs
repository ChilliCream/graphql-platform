using System.Runtime.CompilerServices;

namespace HotChocolate.Types.NodaTime.Tests;

internal static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Initialize()
    {
        CookieCrumbleXunit.Initialize();
    }
}
