using System.Runtime.CompilerServices;

namespace HotChocolate.PersistedOperations.InMemory;

internal static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Initialize()
    {
        CookieCrumbleXunit.Initialize();
    }
}
