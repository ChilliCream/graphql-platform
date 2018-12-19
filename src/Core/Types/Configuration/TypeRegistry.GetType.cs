using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Utilities;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Configuration
{
    internal partial class TypeRegistry
        : ITypeRegistry
    {
        public T GetType<T>(NameString typeName)
            where T : IType
        {
            return TryGetType(typeName, out T type) ? type : default;
        }

        public T GetType<T>(TypeReference typeReference)
            where T : IType
        {
            return TryGetType(typeReference, out T type) ? type : default;
        }

        public bool TryGetType<T>(NameString typeName, out T type)
            where T : IType
        {
            if (string.IsNullOrEmpty(typeName))
            {
                throw new ArgumentException(
                    "The type name mustn't be null or empty.",
                    nameof(typeName));
            }

            if (_namedTypes.TryGetValue(typeName, out INamedType namedType)
                && namedType is T t)
            {
                type = t;
                return true;
            }

            type = default;
            return false;
        }

        public bool TryGetType<T>(TypeReference typeReference, out T type)
            where T : IType
        {
            if (typeReference == null)
            {
                throw new ArgumentNullException(nameof(typeReference));
            }

            if (typeReference.IsClrTypeReference())
            {
                return TryGetTypeFromClrType(
                    typeReference.ClrType,
                    typeReference.Context,
                    out type);
            }

            return TryGetTypeFromAst(typeReference.Type, out type);
        }

        public IEnumerable<Type> GetResolverTypes(NameString typeName)
        {
            if (_resolverTypes.Count > 0)
            {
                foreach (Type resolverType in _resolverTypes.ToArray())
                {
                    IEnumerable<GraphQLResolverOfAttribute> attributes =
                        resolverType.GetCustomAttributes(
                            typeof(GraphQLResolverOfAttribute), false)
                        .OfType<GraphQLResolverOfAttribute>();

                    var all = true;

                    foreach (GraphQLResolverOfAttribute attribute in attributes)
                    {
                        all &= AddResolverTypeToLookup(resolverType, attribute);
                    }

                    if (all)
                    {
                        _resolverTypes.Remove(resolverType);
                    }
                }
            }

            if (_resolverTypeDict.TryGetValue(typeName, out List<Type> types))
            {
                return types;
            }

            return Array.Empty<Type>();
        }

        private bool AddResolverTypeToLookup(
            Type resolverType,
            GraphQLResolverOfAttribute attribute)
        {
            if (attribute.Types == null)
            {
                AddResolverTypeToLookup(resolverType, attribute.TypeNames);
                return true;
            }

            return AddResolverTypeToLookup(resolverType, attribute.Types);
        }

        private bool AddResolverTypeToLookup(
            Type resolverType,
            IEnumerable<Type> types)
        {
            var all = true;

            foreach (Type type in types)
            {
                all &= AddResolverTypeToLookup(resolverType, type);
            }

            return all;
        }

        private bool AddResolverTypeToLookup(
            Type resolverType,
            Type clrType)
        {
            if (_clrTypeToSchemaType.TryGetValue(clrType,
                out NameString typeName))
            {
                AddResolverTypeToLookup(resolverType, typeName);
                return true;
            }

            if (_clrTypes.TryGetValue(clrType,
                out HashSet<NameString> typeNames))
            {
                ObjectType objectType = GetNamedTypes(typeNames)
                    .OfType<ObjectType>().FirstOrDefault();
                if (objectType != null)
                {
                    AddResolverTypeToLookup(resolverType, objectType.Name);
                    return true;
                }
            }

            return false;
        }

        private void AddResolverTypeToLookup(
            Type resolverType,
            IEnumerable<string> typeNames)
        {
            foreach (var typeName in typeNames)
            {
                AddResolverTypeToLookup(resolverType, typeName);
            }
        }

        private void AddResolverTypeToLookup(
            Type resolverType,
            NameString typeName)
        {
            if (!_resolverTypeDict.TryGetValue(typeName, out List<Type> types))
            {
                types = new List<Type>();
                _resolverTypeDict[typeName] = types;
            }

            if (!types.Contains(resolverType))
            {
                types.Add(resolverType);
            }
        }

        private bool TryGetTypeFromClrType<T>(
            Type clrType,
            TypeContext context,
            out T type)
        {
            Type unwrappedClrType = DotNetTypeInfoFactory.Unwrap(clrType);

            if (!unwrappedClrType.IsValueType
                && TryGetTypeFromClrType(
                    unwrappedClrType, context,
                    t => t, out type))
            {
                return true;
            }

            if (_typeInspector.TryCreate(clrType, out TypeInfo typeInfo))
            {
                return TryGetTypeFromClrTypeReference(
                        typeInfo.ClrType, context,
                        typeInfo.TypeFactory, out type)
                    || TryGetTypeFromClrType(
                        typeInfo.ClrType, context,
                        typeInfo.TypeFactory, out type);
            }

            type = default;
            return false;
        }

        private bool TryGetTypeFromClrTypeReference<T>(
            Type clrType,
            TypeContext context,
            Func<INamedType, IType> factory,
            out T type)
        {
            if (_clrTypeToSchemaType.TryGetValue(clrType, out var typeName)
                && _namedTypes.TryGetValue(typeName, out var namedType))
            {
                return TryCreateType(namedType, context, factory, out type);
            }

            type = default;
            return false;
        }

        private bool TryGetTypeFromClrType<T>(
            Type clrType,
            TypeContext context,
            Func<INamedType, IType> factory,
            out T type)
        {
            if (_clrTypes.TryGetValue(clrType, out var namedTypeNames))
            {
                var namedTypes = GetNamedTypes(namedTypeNames).ToList();

                foreach (INamedType namedType in namedTypes)
                {
                    if (TryCreateType(namedType, context, factory, out type))
                    {
                        return true;
                    }
                }
            }

            type = default;
            return false;
        }

        private bool TryCreateType<T>(
            INamedType namedType,
            TypeContext context,
            Func<INamedType, IType> factory,
            out T type)
        {
            if (DoesTypeApplyToContext(namedType, context))
            {
                IType internalType = factory(namedType);
                if (internalType is T t)
                {
                    type = t;
                    return true;
                }
            }

            type = default(T);
            return false;
        }

        private static bool DoesTypeApplyToContext(
            INamedType type,
            TypeContext context)
        {
            switch (context)
            {
                case TypeContext.Output:
                    return type is IOutputType;
                case TypeContext.Input:
                    return type is IInputType;
                default:
                    throw new NotSupportedException();
            }
        }

        private bool TryGetTypeFromAst<T>(ITypeNode typeNode, out T type)
            where T : IType
        {
            if (TryGetTypeFromAst(typeNode, out IType internalType)
                && internalType is T t)
            {
                type = t;
                return true;
            }

            type = default;
            return false;
        }

        private bool TryGetTypeFromAst(ITypeNode typeNode, out IType type)
        {
            if (typeNode.Kind == NodeKind.NonNullType
                && TryGetTypeFromAst(
                    ((NonNullTypeNode)typeNode).Type,
                    out type))
            {
                type = new NonNullType(type);
                return true;
            }

            if (typeNode.Kind == NodeKind.ListType
                && TryGetTypeFromAst(((ListTypeNode)typeNode).Type, out type))
            {
                type = new ListType(type);
                return true;
            }

            if (typeNode.Kind == NodeKind.NamedType
                && TryGetType(((NamedTypeNode)typeNode).Name.Value,
                    out INamedType namedType))
            {
                type = namedType;
                return true;
            }

            type = default;
            return false;
        }

        private IEnumerable<INamedType> GetNamedTypes(
            IEnumerable<NameString> typeNames)
        {
            foreach (var typeName in typeNames)
            {
                if (_namedTypes.TryGetValue(typeName, out INamedType namedType))
                {
                    yield return namedType;
                }
            }
        }

        public IEnumerable<INamedType> GetTypes()
        {
            return _namedTypes.Values;
        }

        public IEnumerable<TypeReference> GetUnresolvedTypes()
        {
            foreach (TypeReference unresolvedType in _unresolvedTypes.ToArray())
            {
                if (IsTypeResolved(unresolvedType))
                {
                    _unresolvedTypes.Remove(unresolvedType);
                }
            }
            return _unresolvedTypes;
        }

        private bool IsTypeResolved(TypeReference typeReference) =>
            IsTypeResolved(typeReference.ClrType, typeReference.Context);


        private bool IsTypeResolved(Type clrType, TypeContext context)
        {
            if (_clrTypes.TryGetValue(clrType,
                out HashSet<NameString> associated))
            {
                foreach (NameString name in associated)
                {
                    switch (context)
                    {
                        case TypeContext.Input:
                            if (IsInputType(name))
                            {
                                return true;
                            }
                            break;

                        case TypeContext.Output:
                            if (IsOutputType(name))
                            {
                                return true;
                            }
                            break;

                        default:
                            throw new NotSupportedException();
                    }
                }
            }
            return false;
        }

        private bool IsInputType(NameString name)
        {
            if (_namedTypes.TryGetValue(name, out INamedType namedType)
                && namedType is IInputType)
            {
                return true;
            }
            return false;
        }

        private bool IsOutputType(NameString name)
        {
            if (_namedTypes.TryGetValue(name, out INamedType namedType)
                && namedType is IOutputType)
            {
                return true;
            }
            return false;
        }
    }
}
