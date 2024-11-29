using System.Runtime.CompilerServices;

namespace HotChocolate.Data.MongoDb.Paging;

internal static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Initialize()
    {
        CookieCrumbleXunit.Initialize();
    }
}
