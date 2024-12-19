using CookieCrumble.Formatters;
using HotChocolate.Fusion.Formatters;

// ReSharper disable once CheckNamespace
namespace HotChocolate.Fusion;

internal static partial class ModuleInitializer
{
    static partial void OnInitialize(Action<ISnapshotValueFormatter> register)
    {
        register(new GraphQLSnapshotValueFormatter());
    }
}
