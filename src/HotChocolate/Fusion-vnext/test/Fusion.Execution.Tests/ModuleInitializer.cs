using System.Runtime.CompilerServices;
using CookieCrumble.HotChocolate.Formatters;

namespace HotChocolate.Fusion;

internal static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Initialize()
    {
        CookieCrumbleTUnit.Initialize();
        Snapshot.RegisterFormatter(new GraphQLSnapshotValueFormatter());
    }
}
