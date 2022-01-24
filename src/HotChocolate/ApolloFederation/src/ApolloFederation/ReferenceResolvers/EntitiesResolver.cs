using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using static HotChocolate.ApolloFederation.WellKnownContextData;

namespace HotChocolate.ApolloFederation;

internal static class EntitiesResolver
{
    public static async Task<List<object?>> _Entities(
        ISchema schema,
        IReadOnlyList<Representation> representations,
        IResolverContext context)
    {
        var entities = new List<object?>();

        foreach (Representation representation in representations)
        {
            if (schema.TryGetType<ObjectType>(representation.TypeName, out var objectType) &&
                objectType.ContextData.TryGetValue(EntityResolver, out var value) &&
                value is FieldResolverDelegate resolver)
            {
                context.SetLocalValue(TypeField, objectType);
                context.SetLocalValue(DataField, representation.Data);
                entities.Add(await resolver.Invoke(context).ConfigureAwait(false));
            }
            else
            {
                throw ThrowHelper.EntityResolver_NoResolverFound();
            }
        }

        return entities;
    }
}
