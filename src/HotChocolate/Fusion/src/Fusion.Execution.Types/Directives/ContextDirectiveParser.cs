using System.Collections.Immutable;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Types.Directives;

/*
directive @fusion__context(
    schema: fusion__Schema!
    name: String!
) repeatable on OBJECT | INTERFACE | UNION
*/
internal static class ContextDirectiveParser
{
    public static ImmutableDictionary<SchemaKey, ImmutableHashSet<string>> Parse(
        IReadOnlyList<DirectiveNode> directiveNodes)
    {
        Dictionary<SchemaKey, ImmutableHashSet<string>.Builder>? temp = null;

        foreach (var directive in directiveNodes)
        {
            if (!directive.Name.Value.Equals(FusionBuiltIns.Context, StringComparison.Ordinal))
            {
                continue;
            }

            var schemaValue = directive.Arguments.FirstOrDefault(t => t.Name.Value == "schema")?.Value;
            var nameValue = directive.Arguments.FirstOrDefault(t => t.Name.Value == "name")?.Value;

            if (schemaValue is not EnumValueNode { Value: { Length: > 0 } schemaName })
            {
                throw new InvalidOperationException(
                    $"The directive `@fusion__context` has an invalid value for `schema`.\r\n{directive}");
            }

            if (nameValue is not StringValueNode { Value: { Length: > 0 } contextName })
            {
                throw new InvalidOperationException(
                    $"The directive `@fusion__context` has an invalid value for `name`.\r\n{directive}");
            }

            temp ??= [];

            var schemaKey = new SchemaKey(schemaName);

            if (!temp.TryGetValue(schemaKey, out var contexts))
            {
                contexts = ImmutableHashSet.CreateBuilder<string>(StringComparer.Ordinal);
                temp.Add(schemaKey, contexts);
            }

            contexts.Add(contextName);
        }

        if (temp is null)
        {
#if NET10_0_OR_GREATER
            return [];
#else
            return ImmutableDictionary<SchemaKey, ImmutableHashSet<string>>.Empty;
#endif
        }

        return temp.ToImmutableDictionary(t => t.Key, t => t.Value.ToImmutable());
    }
}
