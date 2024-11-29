using System.Runtime.CompilerServices;

namespace HotChocolate.ApolloFederation;

internal static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Initialize()
    {
        CookieCrumbleHotChocolate.Initialize();
    }
}
