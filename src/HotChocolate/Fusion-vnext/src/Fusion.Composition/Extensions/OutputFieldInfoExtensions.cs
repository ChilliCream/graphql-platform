using System.Collections.Immutable;
using HotChocolate.Fusion.Info;
using static HotChocolate.Fusion.WellKnownArgumentNames;
using static HotChocolate.Fusion.WellKnownDirectiveNames;

namespace HotChocolate.Fusion.Extensions;

internal static class OutputFieldInfoExtensions
{
    public static bool IsOverridden(
        this OutputFieldInfo fieldInfo,
        ImmutableArray<OutputFieldInfo> fieldGroup)
    {
        var overriddenInSchemaNames =
            fieldGroup
                .Where(i => i.Field.Directives.ContainsName(Override))
                .Select(i => (string)i.Field.Directives[Override].First().Arguments[From].Value!)
                .ToImmutableArray();

        return overriddenInSchemaNames.Contains(fieldInfo.Schema.Name);
    }
}
