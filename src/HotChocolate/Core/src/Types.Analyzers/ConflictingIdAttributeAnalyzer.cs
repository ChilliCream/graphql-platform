using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace HotChocolate.Types.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ConflictingIdAttributeAnalyzer : DiagnosticAnalyzer
{
    // Every [ID] member without a type name shares the declaring type's inferred GraphQL type name.
    // Since each named type is analyzed in isolation, a single constant key is sufficient.
    private const string InferredTypeNameKey = "inferred";

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        [Errors.ConflictingIdAttribute];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
    }

    private static void AnalyzeNamedType(SymbolAnalysisContext context)
    {
        var namedType = (INamedTypeSymbol)context.Symbol;

        if (namedType.TypeKind is not (TypeKind.Class or TypeKind.Struct or TypeKind.Interface))
        {
            return;
        }

        // Group all [ID] members of the type by their effective GraphQL type-name key,
        // preserving source order within each group.
        Dictionary<string, List<IdMember>>? groups = null;

        foreach (var member in namedType.GetMembers())
        {
            ITypeSymbol? memberType = member switch
            {
                IPropertySymbol property => property.Type,
                IMethodSymbol { MethodKind: MethodKind.Ordinary } method => method.ReturnType,
                _ => null
            };

            if (memberType is null)
            {
                continue;
            }

            var idAttribute = GetIdAttribute(member);
            if (idAttribute is null)
            {
                continue;
            }

            var idMember = new IdMember(member, UnwrapRuntimeIdType(memberType), idAttribute.Value.Attribute);

            groups ??= [];
            if (!groups.TryGetValue(idAttribute.Value.TypeNameKey, out var members))
            {
                members = [];
                groups[idAttribute.Value.TypeNameKey] = members;
            }

            members.Add(idMember);
        }

        if (groups is null)
        {
            return;
        }

        foreach (var group in groups.Values)
        {
            AnalyzeGroup(context, group);
        }
    }

    private static void AnalyzeGroup(SymbolAnalysisContext context, List<IdMember> group)
    {
        if (group.Count < 2)
        {
            return;
        }

        // The authoritative member is the [ID] member named "Id" (case-insensitive)
        // if one exists, otherwise the first [ID] member in source order.
        var authoritative = group[0];
        foreach (var candidate in group)
        {
            if (string.Equals(candidate.Member.Name, "Id", StringComparison.OrdinalIgnoreCase))
            {
                authoritative = candidate;
                break;
            }
        }

        foreach (var idMember in group)
        {
            if (ReferenceEquals(idMember, authoritative))
            {
                continue;
            }

            if (SymbolEqualityComparer.Default.Equals(idMember.RuntimeIdType, authoritative.RuntimeIdType))
            {
                continue;
            }

            var syntaxReference = idMember.Attribute.ApplicationSyntaxReference;
            if (syntaxReference is null)
            {
                continue;
            }

            var diagnostic = Diagnostic.Create(
                Errors.ConflictingIdAttribute,
                syntaxReference.GetSyntax(context.CancellationToken).GetLocation(),
                idMember.Member.Name,
                authoritative.Member.Name,
                authoritative.RuntimeIdType.ToDisplayString());

            context.ReportDiagnostic(diagnostic);
        }
    }

    private static (string TypeNameKey, AttributeData Attribute)? GetIdAttribute(ISymbol member)
    {
        foreach (var attribute in member.GetAttributes())
        {
            var attributeClass = attribute.AttributeClass;
            if (attributeClass is null)
            {
                continue;
            }

            if (attributeClass.Name != "IDAttribute"
                || attributeClass.ContainingNamespace?.ToDisplayString() != "HotChocolate.Types.Relay")
            {
                continue;
            }

            // The generic IDAttribute<T> derives its type name from the type argument.
            if (attributeClass is { IsGenericType: true, TypeArguments.Length: 1 })
            {
                return ("generic:" + attributeClass.TypeArguments[0].ToDisplayString(), attribute);
            }

            // An explicit string type name forms its own group.
            if (attribute.ConstructorArguments.Length > 0
                && attribute.ConstructorArguments[0].Value is string typeName)
            {
                return ("literal:" + typeName, attribute);
            }

            // No type name was supplied, so the GraphQL type name is inferred from the
            // declaring type and shared across all such [ID] members of that type.
            return (InferredTypeNameKey, attribute);
        }

        return null;
    }

    private static ITypeSymbol UnwrapRuntimeIdType(ITypeSymbol type)
    {
        while (true)
        {
            if (type is IArrayTypeSymbol arrayType)
            {
                type = arrayType.ElementType;
                continue;
            }

            if (type is INamedTypeSymbol { IsGenericType: true } namedType)
            {
                var constructedFrom = namedType.ConstructedFrom.ToDisplayString();

                if (constructedFrom is "System.Threading.Tasks.Task<TResult>"
                    or "System.Threading.Tasks.ValueTask<TResult>"
                    or "System.Nullable<T>")
                {
                    type = namedType.TypeArguments[0];
                    continue;
                }

                var elementType = GetEnumerableElementType(namedType);
                if (elementType is not null)
                {
                    type = elementType;
                    continue;
                }
            }

            return type;
        }
    }

    private static ITypeSymbol? GetEnumerableElementType(INamedTypeSymbol namedType)
    {
        // Strings implement IEnumerable<char> but must be treated as scalar IDs.
        if (namedType.SpecialType == SpecialType.System_String)
        {
            return null;
        }

        if (namedType.ConstructedFrom.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T)
        {
            return namedType.TypeArguments[0];
        }

        foreach (var @interface in namedType.AllInterfaces)
        {
            if (@interface.ConstructedFrom.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T)
            {
                return @interface.TypeArguments[0];
            }
        }

        return null;
    }

    private sealed record IdMember(ISymbol Member, ITypeSymbol RuntimeIdType, AttributeData Attribute);
}
