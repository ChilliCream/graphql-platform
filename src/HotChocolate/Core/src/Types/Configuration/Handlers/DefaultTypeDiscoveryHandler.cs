using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using HotChocolate.Internal;
using HotChocolate.Types.Helpers;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using System.Diagnostics;

namespace HotChocolate.Configuration;

internal sealed class DefaultTypeDiscoveryHandler(ITypeInspector typeInspector) : TypeDiscoveryHandler
{
    private ITypeInspector TypeInspector { get; } =
        typeInspector ?? throw new ArgumentNullException(nameof(typeInspector));

    public override bool TryInferType(
        TypeReference typeReference,
        TypeDiscoveryInfo typeInfo,
        [NotNullWhen(true)] out TypeReference[]? schemaTypeRefs)
    {
        if (TryCreateKeyValuePairTypeRef(typeReference, typeInfo, out schemaTypeRefs))
        {
            return true;
        }

        TypeReference? schemaType;

        if (typeInfo.IsStatic)
        {
            if (IsStaticObjectTypeExtension(typeInfo))
            {
                var typeExtension = new StaticObjectTypeExtension(typeInfo.RuntimeType);
                schemaType = TypeReference.Create(typeExtension, typeReference.Scope);
            }
            else
            {
                // we only allow static classes for object type extensions,
                // which are already handled above. All other static types
                // cannot be inferred.
                schemaTypeRefs = null;
                return false;
            }
        }
        else if (IsObjectTypeExtension(typeInfo))
        {
            schemaType =
                TypeInspector.CreateTypeRef(
                    typeof(ObjectTypeExtension<>),
                    typeInfo,
                    typeReference);
        }
        else if (IsUnionType(typeInfo))
        {
            schemaType =
                TypeInspector.CreateTypeRef(
                    typeof(UnionType<>),
                    typeInfo,
                    typeReference);
        }
        else if (IsInterfaceType(typeInfo))
        {
            schemaType =
                TypeInspector.CreateTypeRef(
                    typeof(InterfaceType<>),
                    typeInfo,
                    typeReference);
        }
        else if (IsObjectType(typeInfo))
        {
            schemaType =
                TypeInspector.CreateTypeRef(
                    typeof(ObjectType<>),
                    typeInfo,
                    typeReference);
        }
        else if (IsInputObjectType(typeInfo))
        {
            schemaType =
                TypeInspector.CreateTypeRef(
                    typeof(InputObjectType<>),
                    typeInfo,
                    typeReference);
        }
        else if (IsEnumType(typeInfo))
        {
            schemaType =
                TypeInspector.CreateTypeRef(
                    typeof(EnumType<>),
                    typeInfo,
                    typeReference);
        }
        else if (IsDirectiveType(typeInfo))
        {
            schemaType =
                TypeInspector.CreateTypeRef(
                    typeof(DirectiveType<>),
                    typeInfo,
                    typeReference);
        }
        else
        {
            schemaTypeRefs = null;
            return false;
        }

        schemaTypeRefs = [schemaType];
        return true;
    }

    private static bool TryCreateKeyValuePairTypeRef(
        TypeReference typeReference,
        TypeDiscoveryInfo typeInfo,
        [NotNullWhen(true)] out TypeReference[]? schemaTypeRefs)
    {
        // Only extended type references can represent dictionaries.
        if (typeReference is not ExtendedTypeReference { Type: { } extendedType })
        {
            schemaTypeRefs = null;
            return false;
        }

        // We only handle generic KeyValuePair<TKey, TValue> types here.
        if (!extendedType.IsGeneric
            || extendedType.Definition != typeof(KeyValuePair<,>))
        {
            schemaTypeRefs = null;
            return false;
        }

        // For output types we create an object type to represent the key-value pair.
        if (typeInfo.Context is TypeContext.Output or TypeContext.None)
        {
            var typeName = CreateKeyValuePairTypeName(
                extendedType,
                TypeKind.Object);

            schemaTypeRefs =
            [
                TypeReference.Create(
                    typeName,
                    typeReference,
                    _ => CreateKeyValuePairObjectType(extendedType, typeName),
                    typeReference.Context,
                    typeReference.Scope)
            ];
            return true;
        }

        // For input types we create an input object type instead.
        if (typeInfo.Context is TypeContext.Input)
        {
            var typeName = CreateKeyValuePairTypeName(
                extendedType,
                TypeKind.InputObject);

            schemaTypeRefs =
            [
                TypeReference.Create(
                    typeName,
                    typeReference,
                    _ => CreateKeyValuePairInputObjectType(extendedType, typeName),
                    typeReference.Context,
                    typeReference.Scope)
            ];
            return true;
        }

        // We should never get here as all context options are exhausted above.
        Debug.Fail("Unexpected TypeContext value.");
        schemaTypeRefs = null;
        return false;
    }

    private static ObjectType CreateKeyValuePairObjectType(
        IExtendedType keyValuePairType,
        string typeName)
    {
        var runtimeType = keyValuePairType.Type;
        var keyType = keyValuePairType.TypeArguments[0];
        var valueType = keyValuePairType.TypeArguments[1];
        var keyProperty = runtimeType.GetProperty("Key")!;
        var valueProperty = runtimeType.GetProperty("Value")!;

        return new ObjectType(
            descriptor =>
            {
                descriptor.Name(typeName);

                descriptor.Field(keyProperty)
                    .Name("key")
                    .Extend()
                    .OnBeforeCreate(
                        (_, field) => field.SetMoreSpecificType(keyType, TypeContext.Output));

                descriptor.Field(valueProperty)
                    .Name("value")
                    .Extend()
                    .OnBeforeCreate(
                        (_, field) => field.SetMoreSpecificType(valueType, TypeContext.Output));

                descriptor.Extend()
                    .OnBeforeCreate(
                        (_, type) =>
                        {
                            type.RuntimeType = runtimeType;
                            type.FieldBindingType = typeof(object);
                        });
            });
    }

    private static InputObjectType CreateKeyValuePairInputObjectType(
        IExtendedType keyValuePairType,
        string typeName)
    {
        var runtimeType = keyValuePairType.Type;
        var keyType = keyValuePairType.TypeArguments[0];
        var valueType = keyValuePairType.TypeArguments[1];
        var keyProperty = runtimeType.GetProperty("Key")!;
        var valueProperty = runtimeType.GetProperty("Value")!;
        var keyGetter = keyProperty.GetMethod!;
        var valueGetter = valueProperty.GetMethod!;

        return new InputObjectType(
            descriptor =>
            {
                descriptor.Name(typeName);

                descriptor.Field("key")
                    .Extend()
                    .OnBeforeCreate(
                        (_, field) => field.SetMoreSpecificType(keyType, TypeContext.Input));

                descriptor.Field("value")
                    .Extend()
                    .OnBeforeCreate(
                        (_, field) => field.SetMoreSpecificType(valueType, TypeContext.Input));

                descriptor.Extend()
                    .OnBeforeCreate(
                        (_, type) =>
                        {
                            type.RuntimeType = runtimeType;
                            type.CreateInstance =
                                values => Activator.CreateInstance(runtimeType, values[0], values[1])!;
                            type.GetFieldData =
                                (obj, values) =>
                                {
                                    values[0] = keyGetter.Invoke(obj, []);
                                    values[1] = valueGetter.Invoke(obj, []);
                                };
                        });
            });
    }

    private static string CreateKeyValuePairTypeName(IExtendedType type, TypeKind kind)
    {
        var keyType = type.TypeArguments[0];
        var valueType = type.TypeArguments[1];
        var keyName = keyType.Type.Name;
        var valueName = valueType.Type.Name;

        if (keyType.IsNullable)
        {
            keyName = $"Nullable{keyName}";
        }

        if (valueType.IsNullable)
        {
            valueName = $"Nullable{valueName}";
        }

        return kind is TypeKind.InputObject
            ? $"KeyValuePairOf{keyName}And{valueName}Input"
            : $"KeyValuePairOf{keyName}And{valueName}";
    }

    public override bool TryInferKind(
        TypeReference typeReference,
        TypeDiscoveryInfo typeInfo,
        out TypeKind typeKind)
    {
        if (IsObjectTypeExtension(typeInfo))
        {
            typeKind = TypeKind.Object;
            return true;
        }

        if (IsUnionType(typeInfo))
        {
            typeKind = TypeKind.Union;
            return true;
        }

        if (IsInterfaceType(typeInfo))
        {
            typeKind = TypeKind.Interface;
            return true;
        }

        if (IsObjectType(typeInfo))
        {
            typeKind = TypeKind.Object;
            return true;
        }

        if (IsInputObjectType(typeInfo))
        {
            typeKind = TypeKind.InputObject;
            return true;
        }

        if (IsEnumType(typeInfo))
        {
            typeKind = TypeKind.Enum;
            return true;
        }

        if (IsDirectiveType(typeInfo))
        {
            typeKind = TypeKind.Directive;
            return true;
        }

        typeKind = default;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsStaticObjectTypeExtension(TypeDiscoveryInfo typeInfo)
        => typeInfo.IsStatic && typeInfo.Attribute is { Kind: TypeKind.Object, IsTypeExtension: true };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsObjectTypeExtension(TypeDiscoveryInfo typeInfo)
        => typeInfo.Attribute is { Kind: TypeKind.Object, IsTypeExtension: true };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsObjectType(TypeDiscoveryInfo typeInfo)
        => !typeInfo.IsDirectiveRef
            && (typeInfo.Attribute is { Kind: TypeKind.Object, IsTypeExtension: false }
                || (typeInfo.Attribute is null && typeInfo.IsComplex))
            && typeInfo is { Context: TypeContext.Output or TypeContext.None };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsUnionType(TypeDiscoveryInfo typeInfo)
        => typeInfo.Attribute is { Kind: TypeKind.Union, IsTypeExtension: false }
            && typeInfo is { Context: TypeContext.Output or TypeContext.None };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsInterfaceType(TypeDiscoveryInfo typeInfo)
        => (typeInfo.Attribute is { Kind: TypeKind.Interface, IsTypeExtension: false }
                || (typeInfo.Attribute is null && typeInfo.IsInterface))
            && typeInfo is { Context: TypeContext.Output or TypeContext.None };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsInputObjectType(TypeDiscoveryInfo typeInfo)
        => (typeInfo.Attribute is { Kind: TypeKind.InputObject, IsTypeExtension: false }
                || (typeInfo.Attribute is null && typeInfo.IsComplex))
            && typeInfo is { IsAbstract: false, Context: TypeContext.Input };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsEnumType(TypeDiscoveryInfo typeInfo)
        => (typeInfo.Attribute is { Kind: TypeKind.Enum, IsTypeExtension: false }
                || (typeInfo.Attribute is null && typeInfo.IsEnum))
            && typeInfo.IsPublic;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsDirectiveType(TypeDiscoveryInfo typeInfo)
        => typeInfo.Attribute is { Kind: TypeKind.Directive, IsTypeExtension: false };
}
