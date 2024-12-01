using System.Runtime.CompilerServices;

namespace HotChocolate.Data.MongoDb.Projections;

internal static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Initialize()
    {
        CookieCrumbleXunit.Initialize();
    }
}
