using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;

namespace Zeus.Resolvers
{
    internal class ReflectionHelper
    {
        private readonly object _sync = new object();
        private ImmutableDictionary<string, ResolverDelegate> _fieldResolvers =
            ImmutableDictionary<string, ResolverDelegate>.Empty;

        private readonly object _fixedInstance;

        public ReflectionHelper(Type type, object fixedInstance)
        {
            _fixedInstance = fixedInstance ?? throw new ArgumentNullException(nameof(fixedInstance));
            Type = type ?? throw new ArgumentNullException(nameof(type));
        }

        public ReflectionHelper(Type type)
        {
            Type = type ?? throw new ArgumentNullException(nameof(type));
        }

        public Type Type { get; }

        public bool CanHandleInstance(object instance)
        {
            return Type.IsInstanceOfType(instance);
        }

        public bool TryGetResolver(string fieldName, out ResolverDelegate resolver)
        {
            if (string.IsNullOrEmpty(fieldName))
            {
                throw new ArgumentException("The field name mustn't be null.", nameof(fieldName));
            }

            if (!_fieldResolvers.TryGetValue(fieldName, out var internalResolver))
            {
                lock (_sync)
                {
                    MemberInfo member = GetMemberInfo(Type, fieldName);
                    if (member == null)
                    {
                        resolver = null;
                        return false;
                    }

                    var memberResolver = _fixedInstance == null
                        ? new MemberResolver(member)
                        : new MemberResolver(member, _fixedInstance);

                    internalResolver = (ctx, ct) => memberResolver.ResolveAsync(ctx, ct);
                    _fieldResolvers = _fieldResolvers.SetItem(fieldName, internalResolver);
                }
            }

            resolver = internalResolver;
            return true;
        }

        private static MemberInfo GetMemberInfo(Type type, string fieldName)
        {
            ILookup<string, MemberInfo> members = type.GetMembers()
                .ToLookup(t => GetMemberName(t), StringComparer.OrdinalIgnoreCase);
            return members[fieldName].FirstOrDefault();
        }

        internal static string GetMemberName(MemberInfo member)
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