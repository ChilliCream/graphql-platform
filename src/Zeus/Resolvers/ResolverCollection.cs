using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Zeus.Resolvers
{
    public class ResolverCollection
        : IResolverCollection
    {
        private readonly object _sync = new object();
        private readonly ImmutableDictionary<FieldReference, Func<IServiceProvider, IResolver>> _resolverFactories;
        private ImmutableDictionary<FieldReference, IResolver> _resolvers = ImmutableDictionary<FieldReference, IResolver>.Empty;
        private ImmutableDictionary<Type, ImmutableDictionary<string, MemberInfo>> _typeFieldMap = ImmutableDictionary<Type, ImmutableDictionary<string, MemberInfo>>.Empty;

        internal ResolverCollection(IDictionary<FieldReference, Func<IServiceProvider, IResolver>> resolverFactories)
        {
            if (resolverFactories == null)
            {
                throw new ArgumentNullException(nameof(resolverFactories));
            }

            _resolverFactories = resolverFactories.ToImmutableDictionary(t => t.Key, t => t.Value);
        }

        public bool TryGetResolver(IServiceProvider serviceProvider,
            string typeName, string fieldName, out IResolver resolver)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            if (typeName == null)
            {
                throw new ArgumentNullException(nameof(typeName));
            }

            if (fieldName == null)
            {
                throw new ArgumentNullException(nameof(fieldName));
            }

            FieldReference fieldReference = FieldReference.Create(typeName, fieldName);
            if (!_resolvers.TryGetValue(fieldReference, out resolver))
            {
                Func<IServiceProvider, IResolver> resolverFactory;
                if (_resolverFactories.TryGetValue(fieldReference, out resolverFactory))
                {
                    lock (_sync)
                    {
                        resolver = resolverFactory(serviceProvider);
                        _resolvers = _resolvers.SetItem(fieldReference, resolver);
                        return true;
                    }
                }
                return false;
            }
            return true;
        }

        public bool TryGetResolver(IServiceProvider serviceProvider, Type type, string fieldName, out IResolver resolver)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (fieldName == null)
            {
                throw new ArgumentNullException(nameof(fieldName));
            }

            FieldReference fieldReference = FieldReference.Create(type.FullName, fieldName);
            if (!_resolvers.TryGetValue(fieldReference, out resolver))
            {
                lock (_sync)
                {
                    CreateTypeFieldMap(type);

                    if (_typeFieldMap.TryGetValue(type, out var fieldMap)
                        && fieldMap.TryGetValue(fieldName, out MemberInfo member))
                    {
                        resolver = new MemberResolver(member);
                        _resolvers = _resolvers.SetItem(fieldReference, resolver);
                        return true;
                    }
                    return false;
                }
            }
            return true;
        }

        private void CreateTypeFieldMap(Type type)
        {
            if (!_typeFieldMap.ContainsKey(type))
            {
                ImmutableDictionary<string, MemberInfo> fieldMappings =
                    CreateMemberFieldNameMap(type.GetMethods())
                    .Concat(CreateMemberFieldNameMap(type.GetProperties()))
                    .ToImmutableDictionary(t => t.Item1, t => t.Item2);

                _typeFieldMap = _typeFieldMap.SetItem(type, fieldMappings);
            }
        }

        private IEnumerable<Tuple<string, MemberInfo>> CreateMemberFieldNameMap(IEnumerable<MemberInfo> members)
        {
            foreach (MemberInfo member in members)
            {
                if (member.IsDefined(typeof(GraphQLNameAttribute)))
                {
                    GraphQLNameAttribute attribute = (GraphQLNameAttribute)member.GetCustomAttribute(typeof(GraphQLNameAttribute));
                    yield return new Tuple<string, MemberInfo>(attribute.Name, member);
                }
                else
                {
                    string fieldName = $"{member.Name.Substring(0, 1).ToLowerInvariant()}{member.Name.Substring(1)}";
                    yield return new Tuple<string, MemberInfo>(fieldName, member);
                }
            }
        }

    }
}