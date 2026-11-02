using System.Collections.Immutable;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Types.Directives;

/*
directive @fusion__interfaceObject(
    schema: fusion__Schema!
) repeatable on INTERFACE
*/
internal static class InterfaceObjectDirectiveParser
{
    public static ImmutableHashSet<SchemaKey> Parse(IReadOnlyList<DirectiveNode> directiveNodes)
    {
        ImmutableHashSet<SchemaKey>.Builder? temp = null;

        foreach (var directive in directiveNodes)
        {
            if (directive.Name.Value.Equals(FusionBuiltIns.InterfaceObject, StringComparison.Ordinal))
            {
                temp ??= ImmutableHashSet.CreateBuilder<SchemaKey>();

                var schemaValue = directive.Arguments.FirstOrDefault(t => t.Name.Value == "schema")?.Value;

                if (schemaValue is not EnumValueNode { Value: { Length: > 0 } schemaName })
                {
                    throw new InvalidOperationException(
                        $"The directive `@fusion__interfaceObject` has an invalid value for `schema`.\r\n{directive}");
                }

                temp.Add(new SchemaKey(schemaName));
            }
        }

        return temp?.ToImmutable() ?? [];
    }
}
