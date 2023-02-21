using HotChocolate.Language;
using HotChocolate.Skimmed;
using HotChocolate.Utilities;

namespace HotChocolate.Fusion.Composition;

public sealed class EnrichObjectTypesMiddleware : IMergeMiddleware
{
    private readonly IObjectTypeMetaDataEnricher[] _enrichers;

    public EnrichObjectTypesMiddleware(IEnumerable<IObjectTypeMetaDataEnricher> enrichers)
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

        foreach (var schema in context.SubGraphs)
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
            var objectTypes = new List<ObjectTypeInfo>();

            foreach (var schema in context.SubGraphs)
            {
                if (schema.Types.TryGetType(typeName, out var type) &&
                    type is ObjectType objectType)
                {
                    objectTypes.Add(new ObjectTypeInfo(objectType, schema));
                }
            }

            if (objectTypes.Count > 0)
            {
                var typeGroup = new ObjectTypeGroup(typeName, objectTypes);

                foreach (var enricher in _enrichers)
                {
                    await enricher.EnrichAsync(typeGroup, context.Abort).ConfigureAwait(false);
                }

                context.ObjectTypes.Add(typeGroup);
            }
        }

        await next(context).ConfigureAwait(false);
    }
}
