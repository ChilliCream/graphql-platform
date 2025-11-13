using System.Diagnostics;
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

        if (typeAttribute is not null)
        {
            foreach (var attributeData in member.GetAttributes())
            {
                // Check if this is GraphQLTypeAttribute or derived (like GraphQLTypeAttribute<T>)
                var attributeClass = attributeData.AttributeClass;
                while (attributeClass is not null)
                {
                    if (SymbolEqualityComparer.Default.Equals(attributeClass.OriginalDefinition, typeAttribute))
                    {
                        // Check constructor arguments
                        if (attributeData.ConstructorArguments.Length > 0)
                        {
                            var argument = attributeData.ConstructorArguments[0];

                            // Type argument (Type type)
                            if (argument is { Kind: TypedConstantKind.Type, Value: ITypeSymbol typeSymbol })
                            {
                                return new SchemaTypeReference(
                                    SchemaTypeReferenceKind.ExtendedTypeReference,
                                    typeSymbol.ToFullyQualified());
                            }

                            // String argument (string typeSyntax)
                            if (argument is { Kind: TypedConstantKind.Primitive, Value: string syntax })
                            {
                                return new SchemaTypeReference(
                                    SchemaTypeReferenceKind.ExtendedTypeReference,
                                    syntax);
                            }
                        }
                    }

                    attributeClass = attributeClass.BaseType;
                }
            }
        }

        Debug.Assert(member.GetReturnType() is not null);

        // First, we unwrap any non-essential wrapper types and IFieldResult implementations.
        var unwrapped = UnwrapNonEssentialTypes(member.GetReturnType()!, compilation);

        // Then we build the GraphQL type string.
        return new SchemaTypeReference(
            SchemaTypeReferenceKind.ExtendedTypeReference,
            $"global::HotChocolate.Internal.SourceGeneratedType<{BuildTypeCore(unwrapped)}>");
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

    private static string BuildTypeCore(ITypeSymbol typeSymbol)
    {
        bool isNullable;
        ITypeSymbol underlyingType;

        // We first check if the type is a nullable value type (int?, Guid?, etc.).
        if (typeSymbol is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } valueType)
        {
            underlyingType = valueType.TypeArguments[0];
            isNullable = true;
        }

        // For reference types we check NullableAnnotation.
        else if (typeSymbol.IsReferenceType)
        {
            underlyingType = typeSymbol;
            isNullable = typeSymbol.NullableAnnotation == NullableAnnotation.Annotated;
        }

        // In all other cases we expect it to be non-null
        else
        {
            underlyingType = typeSymbol;
            isNullable = false;
        }

        if (underlyingType is INamedTypeSymbol namedType && IsListType(namedType))
        {
            var elementType = namedType.TypeArguments[0];
            var innerTypeString = BuildTypeCore(elementType);
            return isNullable
                ? $"global::HotChocolate.Types.ListType<{innerTypeString}>"
                : $"global::HotChocolate.Types.NonNullType<global::HotChocolate.Types.ListType<{innerTypeString}>>";
        }

        var typeName = GetFullyQualifiedTypeName(underlyingType);
        return isNullable
            ? $"global::HotChocolate.Internal.NamedRuntimeType<{typeName}>"
            : $"global::HotChocolate.Types.NonNullType<global::HotChocolate.Internal.NamedRuntimeType<{typeName}>>";
    }

    private static bool IsListType(INamedTypeSymbol namedType)
    {
        if (!namedType.IsGenericType)
        {
            return false;
        }

        var originalDefinition = namedType.OriginalDefinition;
        var fullName = originalDefinition.ToDisplayString();

        return fullName == "System.Collections.Generic.IEnumerable<T>"
            || fullName == "System.Collections.Generic.IReadOnlyCollection<T>"
            || fullName == "System.Collections.Generic.IReadOnlyList<T>"
            || fullName == "System.Collections.Generic.ICollection<T>"
            || fullName == "System.Collections.Generic.IList<T>"
            || fullName == "System.Collections.Generic.ISet<T>"
            || fullName == "System.Linq.IQueryable<T>"
            || fullName == "System.Collections.Generic.IAsyncEnumerable<T>"
            || fullName == "System.IObservable<T>"
            || fullName == "System.Collections.Generic.List<T>"
            || fullName == "System.Collections.ObjectModel.Collection<T>"
            || fullName == "System.Collections.Generic.Stack<T>"
            || fullName == "System.Collections.Generic.HashSet<T>"
            || fullName == "System.Collections.Generic.Queue<T>"
            || fullName == "System.Collections.Concurrent.ConcurrentBag<T>"
            || fullName == "System.Collections.Immutable.ImmutableArray<T>"
            || fullName == "System.Collections.Immutable.ImmutableList<T>"
            || fullName == "System.Collections.Immutable.ImmutableQueue<T>"
            || fullName == "System.Collections.Immutable.ImmutableStack<T>"
            || fullName == "System.Collections.Immutable.ImmutableHashSet<T>"
            || fullName == "HotChocolate.Execution.ISourceStream<T>"
            || fullName == "HotChocolate.IExecutable<T>";
    }

    private static string GetFullyQualifiedTypeName(ITypeSymbol typeSymbol)
    {
        var displayFormat = SymbolDisplayFormat.FullyQualifiedFormat;
        return typeSymbol.ToDisplayString(displayFormat);
    }
}
