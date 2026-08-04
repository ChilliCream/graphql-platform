using HotChocolate.Types;
using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.ApolloFederation;

/// <summary>
/// Rewrites <c>@key</c> directives on entity types to strip the <c>resolvable</c>
/// argument, keeping only the <c>fields</c> argument.
/// </summary>
internal static class RewriteKeyDirectives
{
    /// <summary>
    /// Applies the key directive rewrite to all type definitions in the schema.
    /// </summary>
    /// <param name="schema">
    /// The mutable schema definition to transform in place.
    /// </param>
    public static void Apply(MutableSchemaDefinition schema)
    {
        foreach (var type in schema.Types)
        {
            if (type is not MutableComplexTypeDefinition complexType)
            {
                continue;
            }

            var keyDirectives = complexType.Directives["key"].ToList();

            foreach (var directive in keyDirectives)
            {
                // Check if resolvable argument exists.
                var hasResolvable = directive.Arguments.ContainsName("resolvable");

                if (!hasResolvable)
                {
                    continue;
                }

                // Replace with directive containing only fields argument.
                if (directive.Arguments.TryGetValue("fields", out var fieldsValue))
                {
                    var newDirective = new Directive(
                        directive.Definition,
                        new ArgumentAssignment("fields", fieldsValue));

                    complexType.Directives.Replace(directive, newDirective);
                }
            }
        }
    }
}
