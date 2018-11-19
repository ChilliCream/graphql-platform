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

        private bool TryGetTypeFromClrType<T>(
            Type clrType,
            TypeContext context,
            out T type)
        {
            if (TryGetTypeFromClrType(clrType, context, t => t, out type))
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

        public bool DoesTypeApplyToContext(INamedType type, TypeContext context)
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

        private bool IsTypeResolved(TypeReference unresolvedType)
        {
            if (_clrTypes.TryGetValue(
                unresolvedType.ClrType,
                out HashSet<NameString> associated))
            {
                foreach (NameString name in associated)
                {
                    switch (unresolvedType.Context)
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
