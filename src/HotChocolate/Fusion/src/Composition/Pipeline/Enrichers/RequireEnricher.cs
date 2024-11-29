namespace HotChocolate.Fusion.Composition.Pipeline;

internal sealed class RequireEnricher : IEntityEnricher
{
    public ValueTask EnrichAsync(
        CompositionContext context,
        EntityGroup entity,
        CancellationToken cancellationToken = default)
    {
        var nextId = 0;

        foreach (var (type, schema) in entity.Parts)
        {
            foreach (var field in type.Fields)
            {
                FieldDependency? dependency = null;

                foreach (var argument in field.Arguments)
                {
                    if (argument.ContainsRequireDirective())
                    {
                        var memberRef = new MemberReference(
                            argument,
                            argument.GetRequireDirective().Field);
                        dependency ??= new FieldDependency(++nextId, schema.Name);
                        dependency.Arguments.Add(argument.Name, memberRef);
                    }
                }

                if (dependency is not null)
                {
                    // we remove the dependant fields from the entity as they
                    // are injected by the gateway.
                    foreach (var memberRef in dependency.Arguments.Values)
                    {
                        field.Arguments.Remove(memberRef.Argument);
                    }

                    if (!entity.Metadata.DependantFields.TryGetValue(field.Name, out var deps))
                    {
                        deps = [];
                        entity.Metadata.DependantFields.Add(field.Name, deps);
                    }

                    deps.Add(dependency);
                }
            }
        }

        return default;
    }
}
