using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using static HotChocolate.ApolloFederation.Constants.WellKnownContextData;

namespace HotChocolate.ApolloFederation.Helpers;

/// <summary>
/// This class contains the _entities resolver method.
/// </summary>
internal static class EntitiesResolver
{
    public static async Task<List<object?>> ResolveAsync(
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

                var entity = await resolver.Invoke(context).ConfigureAwait(false);

                if (entity is not null &&
                    objectType!.ContextData.TryGetValue(ExternalSetter, out value) &&
                    value is Action<ObjectType, IValueNode, object> setExternals)
                {
                    setExternals(objectType, representation.Data!, entity);
                }

                entities.Add(entity);
            }
            else
            {
                throw ThrowHelper.EntityResolver_NoResolverFound();
            }
        }

        return entities;
    }
}
