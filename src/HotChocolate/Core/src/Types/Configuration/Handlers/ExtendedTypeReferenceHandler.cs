using System;
using HotChocolate.Internal;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Utilities;
using ExtendedType = HotChocolate.Internal.ExtendedType;

#nullable enable

namespace HotChocolate.Configuration;

internal sealed class ExtendedTypeReferenceHandler : ITypeRegistrarHandler
{
    private readonly ITypeInspector _typeInspector;

    public ExtendedTypeReferenceHandler(ITypeInspector typeInspector)
    {
        _typeInspector = typeInspector;
    }

    public TypeReferenceKind Kind => TypeReferenceKind.ExtendedType;

    public void Handle(ITypeRegistrar typeRegistrar, TypeReference typeReference)
    {
        var typeRef = (ExtendedTypeReference)typeReference;

        if (_typeInspector.TryCreateTypeInfo(typeRef.Type, out var typeInfo) &&
            !ExtendedType.Tools.IsNonGenericBaseType(typeInfo.NamedType))
        {
            if (typeInfo.NamedType == typeof(IExecutable))
            {
                throw ThrowHelper.NonGenericExecutableNotAllowed();
            }

            var namedType = typeInfo.NamedType;

            if (IsTypeSystemObject(namedType))
            {
                var extendedType = _typeInspector.GetType(namedType);
                var namedTypeReference = typeRef.With(extendedType);

                if (!typeRegistrar.IsResolved(namedTypeReference))
                {
                    typeRegistrar.Register(
                        typeRegistrar.CreateInstance(namedType),
                        typeReference.Scope,
                        ExtendedType.Tools.IsGenericBaseType(namedType));
                }
            }
            else
            {
                TryMapToExistingRegistration(
                    typeRegistrar,
                    typeInfo,
                    typeReference.Context,
                    typeReference.Scope);
            }
        }
    }

    private static void TryMapToExistingRegistration(
        ITypeRegistrar typeRegistrar,
        ITypeInfo typeInfo,
        TypeContext context,
        string? scope)
    {
        ExtendedTypeReference? normalizedTypeRef = null;
        var resolved = false;

        foreach (var component in typeInfo.Components)
        {
            normalizedTypeRef = TypeReference.Create(
                component.Type,
                context,
                scope);

            if (typeRegistrar.IsResolved(normalizedTypeRef))
            {
                resolved = true;
                break;
            }
        }

        if (!resolved && normalizedTypeRef is not null)
        {
            typeRegistrar.MarkUnresolved(normalizedTypeRef);
        }
    }

    private static bool IsTypeSystemObject(Type type) =>
        typeof(TypeSystemObjectBase).IsAssignableFrom(type);
}
