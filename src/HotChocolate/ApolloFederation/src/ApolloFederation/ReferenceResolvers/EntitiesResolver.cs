using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using static HotChocolate.ApolloFederation.WellKnownContextData;

namespace HotChocolate.ApolloFederation
{
    internal static class EntitiesResolver
    {
        public static async Task<List<object?>> _Entities(
            ISchema schema,
            IReadOnlyList<Representation> representations, IResolverContext c)
        {
            var ret = new List<object?>();
            foreach (var representation in representations)
            {
                var representationType = schema.Types
                    .SingleOrDefault(type =>
                        type.Name == representation.Typename &&
                        type.ContextData.ContainsKey(ExtendMarker));

                if (representationType != null)
                {
                    if (representationType.ContextData.TryGetValue(EntityResolver, out var obj) &&
                        obj is Func<object, object?> d)
                    {
                        ret.Add(d(representation));
                    }
                    else
                    {
                        throw ThrowHelper.EntityResolver_NoResolverFound();
                    }
                }
                else if (schema.TryGetType(representation.Typename, out ObjectType type) &&
                    type.ContextData.TryGetValue(EntityResolver, out object? o) &&
                    o is FieldResolverDelegate resolver)
                {
                    c.SetLocalValue("data", representation.Data);
                    ret.Add(await resolver.Invoke(c).ConfigureAwait(false));
                }
                else
                {
                    throw ThrowHelper.EntityResolver_NoResolverFound();
                }
            }

            return ret;
        }
    }
}
