using System.Collections.Concurrent;
using System.Diagnostics;
using System.Security.Cryptography;
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

    public static SchemaTypeReference CreateTypeReference(this Compilation compilation, ISymbol member)
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

        // Next, we create a key that describes the type and ensures we are only executing the type factory once.
        var (typeStructure, typeDefinition, isSimpleType) = CreateTypeKey(unwrapped);

        if (isSimpleType)
        {
            return new SchemaTypeReference(
                SchemaTypeReferenceKind.ExtendedTypeReference,
                typeDefinition);
        }

        return new SchemaTypeReference(
            SchemaTypeReferenceKind.FactoryTypeReference,
            typeDefinition,
            typeStructure);
    }

    private static (string TypeStructure, string TypeDefinition, bool IsSimpleType) CreateTypeKey(
        ITypeSymbol unwrappedType)
    {
        bool isNullable;;
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

        if (underlyingType is INamedTypeSymbol namedType && IsListType(namedType))
        {
            var elementType = namedType.TypeArguments[0];
            var (typeStructure, typeDefinition, _) = CreateTypeKey(elementType);

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

            return (typeStructure, typeDefinition, false);
        }

        var typeName = GetFullyQualifiedTypeName(underlyingType);
        var compliantTypeName = MakeGraphQLCompliant(typeName);

        if (isNullable)
        {
            var typeStructure = string.Format(
                "new global::{0}(\"{1}\")",
                WellKnownTypes.NamedTypeNode,
                compliantTypeName);
            return (typeStructure, typeName, IsSimpleType: unwrappedType.IsReferenceType);
        }
        else
        {
            var typeStructure = string.Format(
                "new global::{0}(new global::{1}(\"{2}\"))",
                WellKnownTypes.NonNullTypeNode,
                WellKnownTypes.NamedTypeNode,
                compliantTypeName);
            return (typeStructure, typeName, IsSimpleType: false);
        }
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

    private static bool IsListType(INamedTypeSymbol namedType)
    {
        if (!namedType.IsGenericType)
        {
            return false;
        }

        var originalDefinition = namedType.OriginalDefinition;
        var typeDefinition = GetGenericTypeDefinition(originalDefinition);

        // Check if the type itself is one of the known list interfaces or classes
        if (WellKnownTypes.ListInterfaceTypes.Contains(typeDefinition)
            || WellKnownTypes.ListClassTypes.Contains(typeDefinition))
        {
            return true;
        }

        // Check if the type implements any of the known list interfaces
        foreach (var interfaceType in namedType.AllInterfaces)
        {
            if (!interfaceType.IsGenericType)
            {
                continue;
            }

            var interfaceDefinition = GetGenericTypeDefinition(interfaceType.OriginalDefinition);
            if (WellKnownTypes.ListInterfaceTypes.Contains(interfaceDefinition))
            {
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
                return true;
            }

            currentType = currentType.BaseType;
        }

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
                            var start = i;
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
