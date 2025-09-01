using System.Collections.Immutable;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Types.Directives;

internal static class UnionMemberDirectiveParser
{
    public static ImmutableDictionary<string, ImmutableDictionary<SchemaKey, ImmutableHashSet<string>>> Parse(
        IEnumerable<UnionTypeDefinitionNode> unionTypes)
    {
        Dictionary<string, Dictionary<SchemaKey, ImmutableHashSet<string>.Builder>>? temp = null;

        foreach (var unionType in unionTypes)
        {
            foreach (var directive in unionType.Directives)
            {
                if (!directive.Name.Value.Equals(FusionBuiltIns.UnionMember, StringComparison.Ordinal))
                {
                    continue;
                }

                temp ??= new Dictionary<string, Dictionary<SchemaKey, ImmutableHashSet<string>.Builder>>();

                var schemaValue = directive.Arguments.FirstOrDefault(t => t.Name.Value == "schema")?.Value;
                var memberValue = directive.Arguments.FirstOrDefault(t => t.Name.Value == "member")?.Value;

                if (schemaValue is not EnumValueNode { Value: { Length: > 0 } schemaName })
                {
                    throw new InvalidOperationException(
                        $"The directive `@fusion__implements` has an invalid value for `schema`.\r\n{directive}");
                }

                if (memberValue is not StringValueNode { Value.Length: > 0 } memberName)
                {
                    throw new InvalidOperationException(
                        $"The directive `@fusion__implements` has an invalid value for `interface`.\r\n{directive}");
                }

                if (!temp.TryGetValue(memberName.Value, out var schemaUnionLookup))
                {
                    schemaUnionLookup = new Dictionary<SchemaKey, ImmutableHashSet<string>.Builder>();
                    temp.Add(memberName.Value, schemaUnionLookup);
                }

                var schemaKey = new SchemaKey(schemaName);

                if (!schemaUnionLookup.TryGetValue(schemaKey, out var unionTypeNames))
                {
                    unionTypeNames = ImmutableHashSet.CreateBuilder<string>();
                    schemaUnionLookup.Add(schemaKey, unionTypeNames);
                }

                unionTypeNames.Add(unionType.Name.Value);
            }
        }

        if (temp is null)
        {
            return ImmutableDictionary<string, ImmutableDictionary<SchemaKey, ImmutableHashSet<string>>>.Empty;
        }

        return temp.ToImmutableDictionary(
            a => a.Key,
            a => a.Value.ToImmutableDictionary(
                b => b.Key,
                b => b.Value.ToImmutableHashSet()));
    }
}
