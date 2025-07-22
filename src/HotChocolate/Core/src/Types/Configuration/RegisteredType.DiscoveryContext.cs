using HotChocolate.Features;
using HotChocolate.Internal;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Configurations;

#nullable enable

namespace HotChocolate.Configuration;

internal sealed partial class RegisteredType : ITypeDiscoveryContext
{
    private List<ISchemaError>? _errors;

    public string? Scope { get; }

    public IDescriptorContext DescriptorContext { get; }

    public IFeatureCollection Features => DescriptorContext.Features;

    public IServiceProvider Services => DescriptorContext.Services;

    public ITypeInspector TypeInspector => DescriptorContext.TypeInspector;

    public TypeInterceptor TypeInterceptor { get; }

    IList<TypeDependency> ITypeDiscoveryContext.Dependencies => Dependencies;

    public void ReportError(ISchemaError error)
    {
        ArgumentNullException.ThrowIfNull(error);

        Errors.Add(error);
    }

    public bool TryPredictTypeKind(TypeReference typeRef, out TypeKind kind)
    {
        if (_typeLookup.TryNormalizeReference(typeRef, out var namedTypeRef)
            && _typeRegistry.TryGetType(namedTypeRef, out var registeredType))
        {
            switch (registeredType.Type)
            {
                case ITypeDefinition type:
                    kind = type.Kind;
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

                return DescriptorContext.TryInferSchemaTypeKind(r, out kind);

            case SchemaTypeReference r:
                kind = GetTypeKindFromSchemaType(TypeInspector.GetType(r.Type.GetType()));
                return true;

            default:
                kind = default;
                return false;
        }
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
