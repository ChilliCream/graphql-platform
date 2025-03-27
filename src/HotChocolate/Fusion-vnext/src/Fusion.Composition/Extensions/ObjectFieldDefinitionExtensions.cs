using System.Collections.Immutable;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.WellKnownArgumentNames;
using static HotChocolate.Fusion.WellKnownDirectiveNames;

namespace HotChocolate.Fusion.Extensions;

public static class ObjectFieldDefinitionExtensions
{
    public static void ApplyLookupDirective(this MutableOutputFieldDefinition field)
    {
        field.Directives.Add(new Directive(new MutableDirectiveDefinition(Lookup)));
    }

    public static ImmutableArray<string> GetSchemaNames(this MutableOutputFieldDefinition field)
    {
        var fusionFieldDirectives =
            field.Directives.AsEnumerable().Where(d => d.Name == FusionField);

        return [.. fusionFieldDirectives.Select(d => (string)d.Arguments[Schema].Value!)];
    }
}
