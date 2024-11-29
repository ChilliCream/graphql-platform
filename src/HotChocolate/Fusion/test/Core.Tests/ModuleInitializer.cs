using System.Runtime.CompilerServices;

namespace HotChocolate.Fusion;

internal static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Initialize()
    {
        CookieCrumbleFusion.Initialize();
        CookieCrumbleHotChocolate.Initialize();
    }
}
