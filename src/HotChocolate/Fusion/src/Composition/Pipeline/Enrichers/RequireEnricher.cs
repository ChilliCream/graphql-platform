namespace HotChocolate.Fusion.Composition.Pipeline;

internal sealed class RequireEnricher : IEntityEnricher
{
    public ValueTask EnrichAsync(
        CompositionContext context,
        EntityGroup entity,
        CancellationToken cancellationToken = default)
    {
        foreach (var (type, schema) in entity.Parts)
        {
            foreach (var field in type.Fields)
            {
                FieldDependency? dependency = null;

                foreach (var argument in field.Arguments)
                {
                    if (argument.ContainsIsDirective())
                    {
                        var memberRef = new MemberReference(argument.GetIsDirective(), argument);
                        dependency ??= new FieldDependency(schema.Name);
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
                        deps = new List<FieldDependency>();
                        entity.Metadata.DependantFields.Add(field.Name, deps);
                    }

                    deps.Add(dependency);
                }
            }
        }

        return default;
    }
}
