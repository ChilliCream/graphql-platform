using System.Runtime.CompilerServices;

namespace HotChocolate.PersistedOperations.AzureBlobStorage;

internal static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Initialize()
    {
        CookieCrumbleXunit.Initialize();
        CookieCrumbleHotChocolate.Initialize();
    }
}
