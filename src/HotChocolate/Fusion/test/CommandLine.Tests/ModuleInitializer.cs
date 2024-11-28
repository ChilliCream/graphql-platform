using System.Runtime.CompilerServices;

namespace CommandLine.Tests;

internal static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Initialize()
    {
        CookieCrumbleHotChocolate.Initialize();
    }
}
