using HotChocolate.Skimmed;

namespace HotChocolate.Fusion.Composition.Pipeline;

internal  sealed class EnrichEntityMiddleware : IMergeMiddleware
{
    private readonly IEntityEnricher[] _enrichers;

    public EnrichEntityMiddleware(IEnumerable<IEntityEnricher> enrichers)
    {
        if (enrichers is null)
        {
            throw new ArgumentNullException(nameof(enrichers));
        }

        _enrichers = enrichers.ToArray();
    }

    public async ValueTask InvokeAsync(CompositionContext context, MergeDelegate next)
    {
        var typeNames = new HashSet<string>();
        foreach (var schema in context.Subgraphs)
        {
            foreach (var type in schema.Types)
            {
                if (type == schema.QueryType ||
                    type == schema.MutationType ||
                    type == schema.SubscriptionType)
                {
                    // we ignore root types
                    continue;
                }
                typeNames.Add(type.Name);
            }
        }

        foreach (var typeName in typeNames)
        {
            var objectTypes = new List<EntityPart>();

            foreach (var schema in context.Subgraphs)
            {
                if (schema.Types.TryGetType(typeName, out var type) &&
                    type is ObjectTypeDefinition objectType)
                {
                    objectTypes.Add(new EntityPart(objectType, schema));
                }
            }

            if (objectTypes.Count > 0)
            {
                var typeGroup = new EntityGroup(typeName, objectTypes);

                foreach (var enricher in _enrichers)
                {
                    await enricher.EnrichAsync(context, typeGroup, context.Abort)
                        .ConfigureAwait(false);
                }

                context.Entities.Add(typeGroup);
            }
        }

        await next(context).ConfigureAwait(false);
    }
}
