using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.ApolloFederation
{
    public static class EntitiesResolver
    {
        public static async Task<List<object>> _Entities(
            ISchema schema,
            IReadOnlyList<Representation> representations, IResolverContext c)
        {
            var ret = new List<object>();
            foreach (var representation in representations)
            {
                var representationType = schema.Types
                    .SingleOrDefault(type =>
                        type.Name == representation.Typename &&
                        type.ContextData.ContainsKey(WellKnownContextData.ExtendMarker));
                if (representationType != null)
                {
                    if (representationType.ContextData
                        .TryGetValue(WellKnownContextData.EntityResolver, out object? obj) &&
                        obj is Delegate d)
                    {
                        ret.Add(d.DynamicInvoke(representation));
                    }
                    else
                    {
                        throw ThrowHelper.EntityResolver_NoResolverFound();
                    }
                }
                else if (schema.TryGetType(representation.Typename, out ObjectType type) &&
                        type.ContextData
                            .TryGetValue(WellKnownContextData.EntityResolver, out object? o) &&
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
