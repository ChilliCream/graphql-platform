using System.Collections.Immutable;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Types.Directives;

/*
   directive @fusion__implements(
     schema: fusion__Schema!
     interface: String!
   ) repeatable on OBJECT | INTERFACE
*/
internal static class ImplementsDirectiveParser
{
    public static ImmutableDictionary<string, ImmutableHashSet<string>> Parse(
        IReadOnlyList<DirectiveNode> directiveNodes)
    {
        Dictionary<string, ImmutableHashSet<string>.Builder>? temp = null;

        foreach (var directive in directiveNodes)
        {
            if (directive.Name.Value.Equals(FusionBuiltIns.Implements, StringComparison.Ordinal))
            {
                temp ??= new Dictionary<string, ImmutableHashSet<string>.Builder>();

                var schemaValue = directive.Arguments.FirstOrDefault(t => t.Name.Value == "schema")?.Value;
                var interfaceValue = directive.Arguments.FirstOrDefault(t => t.Name.Value == "interface")?.Value;

                if (schemaValue is not EnumValueNode { Value.Length: > 0 } schemaName)
                {
                    throw new InvalidOperationException(
                        $"The directive `@fusion__implements` has an invalid value for `schema`.\r\n{directive}");
                }

                if (interfaceValue is not StringValueNode { Value.Length: > 0 } interfaceName)
                {
                    throw new InvalidOperationException(
                        $"The directive `@fusion__implements` has an invalid value for `interface`.\r\n{directive}");
                }

                if(!temp.TryGetValue(schemaName.Value, out var implements))
                {
                    implements = ImmutableHashSet.CreateBuilder<string>();
                    temp.Add(schemaName.Value, implements);
                }

                implements.Add(interfaceName.Value);
            }
        }

        return temp is null
            ? ImmutableDictionary<string, ImmutableHashSet<string>>.Empty
            : temp.ToImmutableDictionary(t => t.Key, t => t.Value.ToImmutable());
    }
}
