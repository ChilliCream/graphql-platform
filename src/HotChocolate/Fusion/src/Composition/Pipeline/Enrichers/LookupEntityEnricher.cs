using System.Runtime.CompilerServices;
using HotChocolate.Language;
using HotChocolate.Skimmed;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Composition.Pipeline;

/// <summary>
/// A pipeline enricher that processes entity groups and adds entity resolvers to
/// metadata for all arguments that contain the @ref directive.
/// </summary>
internal sealed class LookupEntityEnricher : IEntityEnricher
{
    /// <inheritdoc />
    public ValueTask EnrichAsync(
        CompositionContext context,
        EntityGroup entity,
        CancellationToken cancellationToken = default)
    {
        foreach (var (type, schema) in entity.Parts)
        {
            // Check if the schema has a query type
            if (schema.QueryType is null)
            {
                continue;
            }

            if (!schema.DirectiveDefinitions.TryGetDirective("is", out var isDirective))
            {
                if(!schema.Types.TryGetType(BuiltIns.String.Name, out var stringType))
                {
                    stringType = BuiltIns.String.Create();
                    schema.Types.Add(stringType);
                }

                isDirective = new DirectiveDefinition("is");
                isDirective.Arguments.Add(new InputFieldDefinition("field", stringType));
                schema.DirectiveDefinitions.Add(isDirective);
            }

            // Loop through each query field
            foreach (var entityResolverField in schema.QueryType.Fields)
            {
                if (entityResolverField.Directives.ContainsName("lookup"))
                {
                    foreach (var argument in entityResolverField.Arguments)
                    {
                        if (!argument.ContainsIsDirective())
                        {
                            argument.Directives.Add(
                                new Directive(
                                    isDirective,
                                    new ArgumentAssignment("field", argument.Name)));
                        }
                    }
                }
            }
        }

        return default;
    }
}
