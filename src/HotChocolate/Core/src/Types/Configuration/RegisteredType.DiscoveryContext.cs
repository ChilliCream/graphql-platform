using System;
using System.Collections.Generic;
using HotChocolate.Internal;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

#nullable enable

namespace HotChocolate.Configuration;

internal sealed partial class RegisteredType : ITypeDiscoveryContext
{
    private readonly List<IDirectiveReference> _directiveReferences = new();
    private List<ISchemaError>? _errors;

    public string? Scope { get; }

    public IDescriptorContext DescriptorContext { get; }

    public IDictionary<string, object?> ContextData => DescriptorContext.ContextData;

    public IServiceProvider Services => DescriptorContext.Services;

    public ITypeInspector TypeInspector => DescriptorContext.TypeInspector;

    public ITypeInterceptor TypeInterceptor { get; }

    IList<TypeDependency> ITypeDiscoveryContext.Dependencies => Dependencies;

    ITypeSystemObject ITypeSystemObjectContext.Type => Type;

    public void ReportError(ISchemaError error)
    {
        if (error is null)
        {
            throw new ArgumentNullException(nameof(error));
        }

        Errors.Add(error);
    }

    public bool TryPredictTypeKind(ITypeReference typeRef, out TypeKind kind)
    {
        if (_typeLookup.TryNormalizeReference(typeRef, out ITypeReference? namedTypeRef) &&
            _typeRegistry.TryGetType(namedTypeRef, out RegisteredType? registeredType))
        {
            switch (registeredType.Type)
            {
                case INamedType namedType:
                    kind = namedType.Kind;
                    return true;

                case DirectiveType:
                    kind = TypeKind.Directive;
                    return true;

                default:
                    kind = default;
                    return false;
            }
        }

        namedTypeRef ??= typeRef;

        switch (namedTypeRef)
        {
            case ExtendedTypeReference r:
                if (Scalars.TryGetScalar(r.Type.Type, out _))
                {
                    kind = TypeKind.Scalar;
                    return true;
                }

                if (r.Type.IsSchemaType)
                {
                    kind = GetTypeKindFromSchemaType(r.Type);
                    return true;
                }

                return SchemaTypeResolver.TryInferSchemaTypeKind(r, out kind);

            case SchemaTypeReference r:
                kind = GetTypeKindFromSchemaType(TypeInspector.GetType(r.Type.GetType()));
                return true;

            default:
                kind = default;
                return false;
        }
    }

    public void RegisterDependency(IDirectiveReference reference)
    {
        if (reference is null)
        {
            throw new ArgumentNullException(nameof(reference));
        }

        _directiveReferences.Add(reference);
    }

    public void RegisterDependencyRange(IEnumerable<IDirectiveReference> references)
    {
        if (references is null)
        {
            throw new ArgumentNullException(nameof(references));
        }

        _directiveReferences.AddRange(references);
    }

    private static TypeKind GetTypeKindFromSchemaType(IExtendedType type)
    {
        if (typeof(ScalarType).IsAssignableFrom(type.Type))
        {
            return TypeKind.Scalar;
        }

        if (typeof(InputObjectType).IsAssignableFrom(type.Type))
        {
            return TypeKind.InputObject;
        }

        if (typeof(EnumType).IsAssignableFrom(type.Type))
        {
            return TypeKind.Enum;
        }

        if (typeof(ObjectType).IsAssignableFrom(type.Type))
        {
            return TypeKind.Object;
        }

        if (typeof(InterfaceType).IsAssignableFrom(type.Type))
        {
            return TypeKind.Interface;
        }

        if (typeof(UnionType).IsAssignableFrom(type.Type))
        {
            return TypeKind.Union;
        }

        if (typeof(DirectiveType).IsAssignableFrom(type.Type))
        {
            return TypeKind.Directive;
        }

        throw new NotSupportedException();
    }
}
