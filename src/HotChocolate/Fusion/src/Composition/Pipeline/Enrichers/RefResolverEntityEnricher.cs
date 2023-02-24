using HotChocolate.Fusion.Composition.Extensions;
using HotChocolate.Language;
using HotChocolate.Skimmed;

namespace HotChocolate.Fusion.Composition;

public sealed class RefResolverEntityEnricher : IEntityEnricher
{
    public ValueTask EnrichAsync(
        CompositionContext context,
        EntityGroup typeGroup,
        CancellationToken cancellationToken = default)
    {
        foreach (var (type, schema) in typeGroup.Parts)
        {
            if (schema.QueryType is not null)
            {
                foreach (var entityResolverField in schema.QueryType.Fields)
                {
                    if ((entityResolverField.Type == type ||
                            entityResolverField.Type.Kind is TypeKind.NonNull &&
                            entityResolverField.Type.InnerType() == type) &&
                        entityResolverField.Arguments.All(t => t.ContainsRefDirective()))
                    {
                        var arguments = new List<ArgumentNode>();

                        var selection = new FieldNode(
                            null,
                            new NameNode(entityResolverField.GetOriginalName()),
                            null,
                            null,
                            Array.Empty<DirectiveNode>(),
                            arguments,
                            null);

                        var resolver = new EntityResolver(selection, type.Name, schema.Name);

                        foreach (var arg in entityResolverField.Arguments)
                        {
                            var directive = arg.GetRefDirective();
                            var var = type.CreateVariableName(directive);
                            arguments.Add(new ArgumentNode(arg.Name, new VariableNode(var)));
                            resolver.Variables.Add(var, arg.CreateVariableField(directive, var));
                        }

                        typeGroup.Metadata.EntityResolvers.Add(resolver);
                    }
                }
            }
        }

        return default;
    }
}
