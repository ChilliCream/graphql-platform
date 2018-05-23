using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate
{
    // TODO : move under configuration?
    internal sealed class SchemaContext
        : ISchemaContext
    {
        private readonly Dictionary<string, List<ObjectType>> _implementsLookup =
            new Dictionary<string, List<ObjectType>>();
        private readonly Dictionary<string, INamedType> _types;
        private readonly Dictionary<string, FieldResolverDelegate> _fieldResolvers
            = new Dictionary<string, FieldResolverDelegate>();
        private readonly IReadOnlyDictionary<string, ResolveType> _customTypeResolver;
        private readonly IReadOnlyDictionary<string, IsOfType> _customIsOfTypeFunctions;
        private readonly Dictionary<string, Type> _typeMappings = new Dictionary<string, Type>();

        public SchemaContext()
            : this(Enumerable.Empty<INamedType>())
        {
        }

        public SchemaContext(
            IEnumerable<INamedType> systemTypes)
            : this(systemTypes,
                new Dictionary<string, ResolveType>(),
                new Dictionary<string, IsOfType>())
        {
        }

        public SchemaContext(
            IEnumerable<INamedType> systemTypes,
            IReadOnlyDictionary<string, ResolveType> customTypeResolvers,
            IReadOnlyDictionary<string, IsOfType> customIsOfTypeFunctions)
        {
            if (systemTypes == null)
            {
                throw new ArgumentNullException(nameof(systemTypes));
            }

            if (customTypeResolvers == null)
            {
                throw new ArgumentNullException(nameof(customTypeResolvers));
            }

            if (customIsOfTypeFunctions == null)
            {
                throw new ArgumentNullException(nameof(customIsOfTypeFunctions));
            }

            _types = systemTypes.ToDictionary(t => t.Name);
            _customTypeResolver = customTypeResolvers;
            _customIsOfTypeFunctions = customIsOfTypeFunctions;
        }

        public bool AreTypesFinal { get; private set; }

        public void RegisterType(INamedType type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (AreTypesFinal)
            {
                throw new InvalidOperationException("All types are finalized.");
            }

            _types.Add(type.Name, type);
        }

        public void RegisterResolvers(IEnumerable<FieldResolver> fieldResolvers)
        {
            if (fieldResolvers == null)
            {
                throw new ArgumentNullException(nameof(fieldResolvers));
            }

            if (AreTypesFinal)
            {
                throw new InvalidOperationException("All types are finalized.");
            }

            foreach (FieldResolver fieldResolver in fieldResolvers)
            {
                string key = $"{fieldResolver.TypeName}.{fieldResolver.FieldName}";
                _fieldResolvers[key] = fieldResolver.Resolver;
            }
        }

        public void RegisterTypeMappings(IEnumerable<KeyValuePair<string, Type>> typeMappings)
        {
            if (typeMappings == null)
            {
                throw new ArgumentNullException(nameof(typeMappings));
            }

            if (AreTypesFinal)
            {
                throw new InvalidOperationException("All types are finalized.");
            }

            foreach (KeyValuePair<string, Type> typeMapping in typeMappings)
            {
                _typeMappings[typeMapping.Key] = typeMapping.Value;
            }
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

        public bool TypeExists<T>(string typeName)
            where T : INamedType
        {
            if (_types.TryGetValue(typeName, out var t)
                && t is T)
            {
                return true;
            }
            return false;
        }

        public INamedType GetType(string typeName)
        {
            if (_types.TryGetValue(typeName, out var t))
            {
                return t;
            }
            throw new ArgumentException(
                "The specified type does not exist.");
        }

        public bool TryGetType(string typeName, out INamedType type)
        {
            if (_types.TryGetValue(typeName, out var t))
            {
                type = t;
                return true;
            }

            type = null;
            return false;
        }

        public T GetType<T>(string typeName)
            where T : INamedType
        {
            if (_types.TryGetValue(typeName, out var t)
                && t is T type)
            {
                return type;
            }
            throw new ArgumentException(
                "The specified type does not exist or is not of the specified type.");
        }

        public bool TryGetType<T>(string typeName, out T type)
            where T : INamedType
        {
            if (_types.TryGetValue(typeName, out var t)
                && t is T it)
            {
                type = it;
                return true;
            }

            type = default(T);
            return false;
        }

        public IReadOnlyCollection<INamedType> GetAllTypes()
        {
            return _types.Values;
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

        public bool TryGetOutputType<T>(string typeName, out T type)
            where T : IOutputType
        {
            if (_types.TryGetValue(typeName, out var t)
                && t is T ot)
            {
                type = ot;
                return true;
            }

            type = default(T);
            return false;
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

        public bool TryGetInputType<T>(string typeName, out T type)
            where T : IInputType
        {
            if (_types.TryGetValue(typeName, out var t)
                && t is T it)
            {
                type = it;
                return true;
            }

            type = default(T);
            return false;
        }

        public FieldResolverDelegate CreateResolver(
            string typeName, string fieldName)
        {
            string key = $"{typeName}.{fieldName}";
            if (_fieldResolvers.TryGetValue(key, out var resolver))
            {
                return resolver;
            }
            return null;
        }

        public IsOfType CreateIsOfType(string typeName)
        {
            if (!_customIsOfTypeFunctions.TryGetValue(
                typeName, out IsOfType isOfType))
            {
                return new IsOfType((c, r) =>
                {
                    if (r == null)
                    {
                        return true;
                    }

                    if (_typeMappings.TryGetValue(typeName, out Type type)
                        && type.IsInstanceOfType(r))
                    {
                        return true;
                    }

                    return false;
                });
            }
            return isOfType;
        }

        public ResolveType CreateTypeResolver(string typeName)
        {
            if (_customTypeResolver.TryGetValue(typeName, out var rt))
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

        internal List<SchemaError> Seal()
        {
            if (AreTypesFinal)
            {
                throw new InvalidOperationException(
                    "Types are already in a final state.");
            }
            AreTypesFinal = true;

            List<SchemaError> errors = new List<SchemaError>();

            // Initialize types in correct order
            CompleteTypeInitialization(
                GetAllTypes().OfType<InterfaceType>(),
                error => errors.Add(error));
            CompleteTypeInitialization(
                GetAllTypes().OfType<InputObjectType>(),
                error => errors.Add(error));
            CompleteTypeInitialization(
                GetAllTypes().OfType<ObjectType>(),
                error => errors.Add(error));
            CompleteTypeInitialization(
                GetAllTypes().OfType<UnionType>(),
                error => errors.Add(error));

            return errors;
        }

        private void CompleteTypeInitialization(
           IEnumerable<ITypeInitializer> initializers,
           Action<SchemaError> reportError)
        {
            foreach (ITypeInitializer initializer in initializers)
            {
                initializer.CompleteInitialization(reportError);
            }
        }
    }
}
