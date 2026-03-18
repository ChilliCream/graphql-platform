using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Types.Analyzers.Models;
using Microsoft.CodeAnalysis;

namespace HotChocolate.Types.Analyzers.Helpers;

public static class TypeReferenceBuilder
{
    private static readonly HashSet<string> s_nonEssentialWrapperTypes =
    [
        "System.Threading.Tasks.ValueTask<T>",
        "System.Threading.Tasks.Task<T>",
        "HotChocolate.Optional<T>"
    ];

    public static SchemaTypeReference CreateTypeReference(
        this Compilation compilation,
        ISymbol member,
        bool isBatchResolver = false)
    {
        var typeAttribute = compilation.GetTypeByMetadataName(WellKnownAttributes.GraphQLTypeAttribute);
        var genericTypeAttribute = compilation.GetTypeByMetadataName(WellKnownAttributes.GraphQLTypeAttribute + "`1");

        if (typeAttribute is not null)
        {
            foreach (var attributeData in member.GetAttributes())
            {
                var attributeClass = attributeData.AttributeClass;

                if (attributeClass is null)
                {
                    continue;
                }

                // we check first if it is the generic type attribute GraphQLTypeAttribute<T>
                if (SymbolEqualityComparer.Default.Equals(attributeClass.OriginalDefinition, genericTypeAttribute))
                {
                    var typeArgument = attributeClass.TypeArguments[0];
                    return new SchemaTypeReference(
                        SchemaTypeReferenceKind.ExtendedTypeReference,
                        typeArgument.ToFullyQualified());
                }

                // next we check if it is the non-generic type attribute GraphQLTypeAttribute
                if (SymbolEqualityComparer.Default.Equals(attributeClass, typeAttribute))
                {
                    // if no constructor args are set we skip the attribute
                    if (attributeData.ConstructorArguments.Length == 0)
                    {
                        continue;
                    }

                    var argument = attributeData.ConstructorArguments[0];

                    if (argument is { Kind: TypedConstantKind.Type, Value: ITypeSymbol typeSymbol })
                    {
                        return new SchemaTypeReference(
                            SchemaTypeReferenceKind.ExtendedTypeReference,
                            typeSymbol.ToFullyQualified());
                    }

                    if (argument is { Kind: TypedConstantKind.Primitive, Value: string syntax })
                    {
                        return new SchemaTypeReference(
                            SchemaTypeReferenceKind.SyntaxTypeReference,
                            syntax);
                    }
                }
            }
        }

        Debug.Assert(member.GetReturnType() is not null);

        // First, we unwrap any non-essential wrapper types and IFieldResult implementations.
        var unwrapped = UnwrapNonEssentialTypes(member.GetReturnType()!, compilation);

        // For batch resolvers, the return type is a list (e.g. List<string>) and we need
        // to unwrap to the element type (e.g. string) for the GraphQL field type.
        if (isBatchResolver)
        {
            unwrapped = UnwrapListElementType(unwrapped) ?? unwrapped;
        }

        // Next, we create a key that describes the type and ensures we are only executing the type factory once.
        var (typeStructure, typeDefinition, nullability, isSimpleType) = CreateTypeKey(unwrapped);

        if (isSimpleType)
        {
            return new SchemaTypeReference(
                SchemaTypeReferenceKind.ExtendedTypeReference,
                typeDefinition,
                nullability: nullability);
        }

        return new SchemaTypeReference(
            SchemaTypeReferenceKind.FactoryTypeReference,
            typeDefinition,
            typeStructure,
            nullability);
    }

    private static (string TypeStructure, string TypeDefinition, string? Nullability, bool IsSimpleType) CreateTypeKey(
        ITypeSymbol unwrappedType)
    {
        bool isNullable;
        ITypeSymbol underlyingType;

        // We first check if the type is a nullable value type (int?, Guid?, etc.).
        if (unwrappedType is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } vt)
        {
            underlyingType = vt.TypeArguments[0];
            isNullable = true;
        }

        // For reference types we check NullableAnnotation.
        else if (unwrappedType.IsReferenceType)
        {
            underlyingType = unwrappedType;
            isNullable = unwrappedType.NullableAnnotation == NullableAnnotation.Annotated;
        }

        // In all other cases we expect it to be non-null
        else
        {
            underlyingType = unwrappedType;
            isNullable = false;
        }

        if (underlyingType is INamedTypeSymbol namedType && TryGetListElementType(namedType, out var listElementType))
        {
            var (typeStructure, typeDefinition, elementNullability, _) = CreateTypeKey(listElementType);

            if (isNullable)
            {
                typeStructure = string.Format(
                    "new global::{0}({1})",
                    WellKnownTypes.ListTypeNode,
                    typeStructure);
            }
            else
            {
                typeStructure = string.Format(
                    "new global::{0}(new global::{1}({2}))",
                    WellKnownTypes.NonNullTypeNode,
                    WellKnownTypes.ListTypeNode,
                    typeStructure);
            }

            return (typeStructure, typeDefinition, elementNullability, false);
        }

        if (IsArrayType(unwrappedType, out var arrayElementType))
        {
            var (typeStructure, typeDefinition, elementNullability, _) = CreateTypeKey(arrayElementType);

            if (isNullable)
            {
                typeStructure = string.Format(
                    "new global::{0}({1})",
                    WellKnownTypes.ListTypeNode,
                    typeStructure);
            }
            else
            {
                typeStructure = string.Format(
                    "new global::{0}(new global::{1}({2}))",
                    WellKnownTypes.NonNullTypeNode,
                    WellKnownTypes.ListTypeNode,
                    typeStructure);
            }

            return (typeStructure, typeDefinition, elementNullability, false);
        }

        var typeName = GetFullyQualifiedTypeName(underlyingType);
        var compliantTypeName = MakeGraphQLCompliant(typeName);
        var nullability = ShouldPreserveNullability(underlyingType)
            ? CreateNullabilityLiteral(underlyingType, isNullable)
            : null;

        if (isNullable)
        {
            var typeStructure = string.Format(
                "new global::{0}(\"{1}\")",
                WellKnownTypes.NamedTypeNode,
                compliantTypeName);
            return (typeStructure, typeName, nullability, IsSimpleType: unwrappedType.IsReferenceType);
        }
        else
        {
            var typeStructure = string.Format(
                "new global::{0}(new global::{1}(\"{2}\"))",
                WellKnownTypes.NonNullTypeNode,
                WellKnownTypes.NamedTypeNode,
                compliantTypeName);
            return (typeStructure, typeName, nullability, IsSimpleType: false);
        }
    }

    private static bool ShouldPreserveNullability(ITypeSymbol typeSymbol)
        => typeSymbol is INamedTypeSymbol { IsGenericType: true };

    private static string CreateNullabilityLiteral(
        ITypeSymbol typeSymbol,
        bool isNullable)
    {
        var flags = new List<string>();
        CollectNullability(typeSymbol, isNullable, flags);

        return flags.Count == 0
            ? "[]"
            : $"[{string.Join(", ", flags)}]";
    }

    private static void CollectNullability(
        ITypeSymbol typeSymbol,
        bool isNullable,
        List<string> flags)
    {
        flags.Add(isNullable ? "true" : "false");

        if (typeSymbol is not INamedTypeSymbol namedType || !namedType.IsGenericType)
        {
            return;
        }

        // Nullable<T> is represented by the wrapped value type and a nullable root flag.
        if (namedType.OriginalDefinition.SpecialType is SpecialType.System_Nullable_T)
        {
            if (namedType.TypeArguments.Length == 1
                && namedType.TypeArguments[0] is INamedTypeSymbol innerNamed
                && innerNamed.IsGenericType)
            {
                foreach (var argument in innerNamed.TypeArguments)
                {
                    CollectNullability(argument, IsNullable(argument), flags);
                }
            }
            return;
        }

        foreach (var argument in namedType.TypeArguments)
        {
            CollectNullability(argument, IsNullable(argument), flags);
        }
    }

    private static bool IsNullable(ITypeSymbol typeSymbol)
    {
        if (typeSymbol is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T })
        {
            return true;
        }

        return typeSymbol.IsReferenceType
            && typeSymbol.NullableAnnotation == NullableAnnotation.Annotated;
    }

    private static ITypeSymbol? UnwrapListElementType(ITypeSymbol typeSymbol)
    {
        if (typeSymbol is IArrayTypeSymbol arrayType)
        {
            return arrayType.ElementType;
        }

        if (typeSymbol is INamedTypeSymbol namedType && TryGetListElementType(namedType, out var elementType))
        {
            return elementType;
        }

        return null;
    }

    private static ITypeSymbol UnwrapNonEssentialTypes(ITypeSymbol typeSymbol, Compilation compilation)
    {
        var fieldResultInterface = compilation.GetFieldResultInterface();

        while (typeSymbol is INamedTypeSymbol { TypeArguments.Length: 1 } namedType)
        {
            var shouldUnwrap = IsNonEssentialWrapperType(namedType)
                || ImplementsFieldResultInterface(namedType, fieldResultInterface);

            if (!shouldUnwrap)
            {
                break;
            }

            typeSymbol = namedType.TypeArguments[0];
        }

        return typeSymbol;
    }

    private static bool IsNonEssentialWrapperType(INamedTypeSymbol namedType)
    {
        var fullName = namedType.OriginalDefinition.ToDisplayString();
        return s_nonEssentialWrapperTypes.Contains(fullName);
    }

    private static bool ImplementsFieldResultInterface(INamedTypeSymbol namedType, INamedTypeSymbol? interfaceType)
    {
        if (interfaceType is null)
        {
            return false;
        }

        foreach (var type in namedType.AllInterfaces)
        {
            if (SymbolEqualityComparer.Default.Equals(type.OriginalDefinition, interfaceType))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsArrayType(ITypeSymbol namedType, [NotNullWhen(true)] out ITypeSymbol? elementType)
    {
        if (namedType is IArrayTypeSymbol arrayType)
        {
            elementType = arrayType.ElementType;
            return true;
        }

        elementType = null;
        return false;
    }

    private static bool TryGetListElementType(
        INamedTypeSymbol namedType,
        [NotNullWhen(true)] out ITypeSymbol? elementType)
    {
        if (!namedType.IsGenericType)
        {
            elementType = null;
            return false;
        }

        var originalDefinition = namedType.OriginalDefinition;
        var typeDefinition = GetGenericTypeDefinition(originalDefinition);

        // Check if the type itself is one of the known list interfaces or classes
        if (WellKnownTypes.ListInterfaceTypes.Contains(typeDefinition)
            || WellKnownTypes.ListClassTypes.Contains(typeDefinition))
        {
            elementType = namedType.TypeArguments[0];
            return true;
        }

        // Check if the type implements any of the known list interfaces.
        // This handles cases like Dictionary<K,V> which implements IEnumerable<KeyValuePair<K,V>>.
        // We extract the element type from the interface, not from the type's own type arguments.
        foreach (var interfaceType in namedType.AllInterfaces)
        {
            if (!interfaceType.IsGenericType)
            {
                continue;
            }

            var interfaceDefinition = GetGenericTypeDefinition(interfaceType.OriginalDefinition);
            if (WellKnownTypes.ListInterfaceTypes.Contains(interfaceDefinition))
            {
                elementType = interfaceType.TypeArguments[0];
                return true;
            }
        }

        // Check if the type or any of its base types is one of the known list classes
        var currentType = namedType.BaseType;
        while (currentType is not null)
        {
            if (!currentType.IsGenericType)
            {
                currentType = currentType.BaseType;
                continue;
            }

            var baseDefinition = GetGenericTypeDefinition(currentType.OriginalDefinition);
            if (WellKnownTypes.ListClassTypes.Contains(baseDefinition))
            {
                elementType = currentType.TypeArguments[0];
                return true;
            }

            currentType = currentType.BaseType;
        }

        elementType = null;
        return false;
    }

    private static string GetGenericTypeDefinition(INamedTypeSymbol typeSymbol)
    {
        // Convert a generic type like "System.Collections.Generic.List<T>"
        // to the definition format "System.Collections.Generic.List<>"
        return typeSymbol.ConstructUnboundGenericType().ToDisplayString();
    }

    private static string GetFullyQualifiedTypeName(ITypeSymbol typeSymbol)
    {
        var displayFormat = SymbolDisplayFormat.FullyQualifiedFormat;
        return typeSymbol.ToDisplayString(displayFormat);
    }

    private static string MakeGraphQLCompliant(string typeName)
    {
        var sb = PooledObjects.GetStringBuilder();
        var i = 0;

        while (i < typeName.Length)
        {
            var c = typeName[i];

            switch (c)
            {
                case '.':
                case ':':
                case '+':
                    sb.Append('_');
                    i++;
                    break;

                case '<':
                    sb.Append("Of");
                    i++;
                    break;

                case '>':
                    i++;
                    break;

                case ',':
                    i++;
                    while (i < typeName.Length && typeName[i] == ' ')
                    {
                        i++;
                    }

                    sb.Append("And");
                    break;

                case '[':
                    if (i + 1 < typeName.Length && typeName[i + 1] == ']')
                    {
                        sb.Append("Array");
                        i += 2;

                        var dimensions = 1;
                        while (i < typeName.Length && typeName[i] == '[')
                        {
                            while (i < typeName.Length && typeName[i] != ']')
                            {
                                i++;
                            }

                            if (i < typeName.Length)
                            {
                                i++;
                            }

                            dimensions++;
                        }

                        if (dimensions > 1)
                        {
                            sb.Append(dimensions).Append('D');
                        }
                    }
                    else
                    {
                        i++;
                    }

                    break;

                case ']':
                    i++;
                    break;

                case '?':
                    sb.Append("Nullable");
                    i++;
                    break;

                case '*':
                    sb.Append("Pointer");
                    i++;
                    break;

                case '(':
                case ')':
                case ' ':
                    i++;
                    break;

                case var ch and (>= 'a' and <= 'z'
                    or >= 'A' and <= 'Z'
                    or >= '0' and <= '9'
                    or '_'):
                    sb.Append(ch);
                    i++;
                    break;

                default:
                    i++;
                    break;
            }
        }

        var normalizedTypeName = sb.ToString();
        PooledObjects.Return(sb);
        return normalizedTypeName;
    }
}
