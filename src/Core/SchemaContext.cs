using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate
{
    internal sealed class SchemaContext
    {
        private readonly Dictionary<string, List<ObjectType>> _implementsLookup =
            new Dictionary<string, List<ObjectType>>();
        private readonly Dictionary<string, INamedType> _types;
        private readonly Dictionary<string, FieldResolverDelegate> _fieldResolvers;
        private readonly IReadOnlyDictionary<string, ResolveType> _typeResolver;
        private readonly IsOfTypeRouter _isOfTypeRouter;

        public SchemaContext(
            IEnumerable<INamedType> systemTypes,
            IEnumerable<FieldResolver> fieldResolvers,
            IReadOnlyDictionary<string, ResolveType> typeResolver,
            IsOfTypeRouter isOfTypeRouter)
        {
            if (systemTypes == null)
            {
                throw new ArgumentNullException(nameof(systemTypes));
            }

            if (fieldResolvers == null)
            {
                throw new ArgumentNullException(nameof(fieldResolvers));
            }

            if (typeResolver == null)
            {
                throw new ArgumentNullException(nameof(typeResolver));
            }

            _types = systemTypes.ToDictionary(t => t.Name);
            _fieldResolvers = fieldResolvers.ToDictionary(
                t => t.TypeName + "." + t.FieldName, t => t.Resolver);
            _typeResolver = typeResolver;
            _isOfTypeRouter = isOfTypeRouter;
        }

        public void Register(INamedType type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }
            _types.Add(type.Name, type);
        }

        private void RegisterLookup(
            string interfaceOrUnionTypeName,
            ObjectType objectType)
        {
            List<ObjectType> types;
            if (!_implementsLookup.TryGetValue(interfaceOrUnionTypeName,
                out types))
            {
                types = new List<ObjectType>();
                _implementsLookup[interfaceOrUnionTypeName] = types;
            }
            types.Add(objectType);
        }

        public IOutputType GetOutputType(string typeName)
        {
            if (_types.TryGetValue(typeName, out var t)
                && t is IOutputType ot)
            {
                return ot;
            }
            throw new ArgumentException(
                "The specified type does not exist or is not an output type.");
        }
        public T GetOutputType<T>(string typeName)
            where T : IOutputType
        {
            if (_types.TryGetValue(typeName, out var t)
                && t is T ot)
            {
                return ot;
            }
            throw new ArgumentException(
                "The specified type does not exist or is " +
                "not of the specified type.");
        }

        public IInputType GetInputType(string typeName)
        {
            if (_types.TryGetValue(typeName, out var t)
                && t is IInputType it)
            {
                return it;
            }
            throw new ArgumentException(
                "The specified type does not exist or is not an output type.");
        }

        public FieldResolverDelegate CreateResolver(
            string typeName, string fieldName)
        {
            string key = $"{typeName}.{fieldName}";
            if (_fieldResolvers.TryGetValue(key, out var resolver))
            {
                return resolver;
            }
            throw new InvalidOperationException(
                "The configuration is missing a resolver.");
        }

        public IsOfType CreateIsOfType(string typeName)
        {
            if (_isOfTypeRouter == null)
            {
                return new IsOfType((c, r) =>
                    c.ObjectType.Name == r.GetType().Name);
            }
            return new IsOfType((c, r) => _isOfTypeRouter(typeName, c, r));
        }

        public ResolveType CreateTypeResolver(string typeName)
        {
            if (_typeResolver.TryGetValue(typeName, out var rt))
            {
                return rt;
            }

            return new ResolveType((c, r) =>
                FallbackTypeResolver(typeName, c, r));
        }

        private ObjectType FallbackTypeResolver(
            string typeName,
            IResolverContext context,
            object resolverResult)
        {
            if (_implementsLookup.TryGetValue(typeName, out var types))
            {
                foreach (ObjectType type in types)
                {
                    if (type.IsOfType(context, resolverResult))
                    {
                        return type;
                    }
                }
            }

            throw new InvalidOperationException(
                "At least one type must match.");
        }


    }
}