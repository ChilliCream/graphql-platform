using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Prometheus.Resolvers
{
    public class DynamicMemberResolver
        : IResolver
    {
        private readonly object _sync = new object();
        private readonly string _propertyName;
        private ImmutableDictionary<Type, ResolverDelegate> _memberResolvers =
            ImmutableDictionary<Type, ResolverDelegate>.Empty;

        public DynamicMemberResolver(string fieldName)
        {
            if (string.IsNullOrEmpty(fieldName))
            {
                throw new ArgumentNullException(nameof(fieldName));
            }

            _propertyName = fieldName;
        }

        public Task<object> ResolveAsync(IResolverContext context, CancellationToken cancellationToken)
        {
            object parent = context.Parent<object>();
            Type parentType = parent.GetType();

            if (!_memberResolvers.TryGetValue(parentType, out var internalResolver))
            {
                ReflectionHelper reflectionHelper = new ReflectionHelper(parentType);
                if (!reflectionHelper.TryGetResolver(_propertyName, out internalResolver))
                {
                    return Task.FromResult<object>(null);
                }

                lock (_sync)
                {
                    _memberResolvers = _memberResolvers.SetItem(parentType, internalResolver);
                }
            }

            return internalResolver(context, cancellationToken);
        }
    }
}