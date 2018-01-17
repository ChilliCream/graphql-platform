using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Zeus
{
    public class ResolverBuilder
        : IResolverBuilder
    {
        private readonly Dictionary<FieldReference, ResolveAsync> _resolvers = new Dictionary<FieldReference, ResolveAsync>();

        private ResolverBuilder() { }

        public IResolverBuilder Add(string typeName, string fieldName, Func<IServiceProvider, IResolverContext, object> resolver)
        {
            if (typeName == null)
            {
                throw new ArgumentNullException(nameof(typeName));
            }

            if (fieldName == null)
            {
                throw new ArgumentNullException(nameof(fieldName));
            }

            if (resolver == null)
            {
                throw new ArgumentNullException(nameof(resolver));
            }

            return Add(typeName, fieldName, (sp, rc, ct) => Task.FromResult(resolver(sp, rc)));
        }

        public IResolverBuilder Add(string typeName, string fieldName, Func<IServiceProvider, IResolverContext, CancellationToken, Task<object>> resolver)
        {
            if (typeName == null)
            {
                throw new ArgumentNullException(nameof(typeName));
            }

            if (fieldName == null)
            {
                throw new ArgumentNullException(nameof(fieldName));
            }

            if (resolver == null)
            {
                throw new ArgumentNullException(nameof(resolver));
            }

            _resolvers[FieldReference.Create(typeName, fieldName)] = new ResolveAsync((sp, rc, ct) => resolver(sp, rc, ct));
            return this;
        }

        public IResolverCollection Build()
        {
            return new ResolverCollection(_resolvers);
        }

        public static IResolverBuilder Create()
        {
            return new ResolverBuilder();
        }
    }

}
