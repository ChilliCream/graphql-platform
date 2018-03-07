using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Zeus.Resolvers
{

    public class DynamicMemberResolver
        : IResolver
    {
        private readonly object _sync = new object();
        private readonly string _propertyName;
        private ImmutableDictionary<Type, MemberResolver> _memberResolvers = ImmutableDictionary<Type, MemberResolver>.Empty;

        public DynamicMemberResolver(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
            {
                throw new ArgumentNullException(nameof(propertyName));
            }

            _propertyName = propertyName;
        }

        public Task<object> ResolveAsync(IResolverContext context, CancellationToken cancellationToken)
        {            
            object parent = context.Parent<object>();
            Type parentType = parent.GetType();

            if (!_memberResolvers.TryGetValue(parentType, out var internalResolver))
            {
                MemberInfo member = GetMemberInfo(parentType);
                if (member == null)
                {
                    return Task.FromResult<object>(null);
                }

                lock (_sync)
                {
                    internalResolver = new MemberResolver(member);
                    _memberResolvers = _memberResolvers.SetItem(parentType, internalResolver);
                }
            }

            return internalResolver.ResolveAsync(context, cancellationToken);
        }

        private MemberInfo GetMemberInfo(Type type)
        {
            Dictionary<string, MemberInfo> members = type.GetMembers()
                .ToDictionary(t => GetMemberName(t), StringComparer.OrdinalIgnoreCase);
            if (members.TryGetValue(_propertyName, out var selectedMember))
            {
                return selectedMember;
            }
            return null;
        }

        private string GetMemberName(MemberInfo member)
        {
            if (member.IsDefined(typeof(GraphQLNameAttribute)))
            {
                var attribute = member.GetCustomAttribute<GraphQLNameAttribute>();
                return attribute.Name;
            }
            return member.Name;
        }
    }
}