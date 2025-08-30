using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Types.Analyzers.Filters;
using HotChocolate.Types.Analyzers.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static System.StringComparison;
using static HotChocolate.Types.Analyzers.WellKnownAttributes;
using TypeInfo = HotChocolate.Types.Analyzers.Models.TypeInfo;

namespace HotChocolate.Types.Analyzers.Inspectors;

public sealed class TypeAttributeInspector : ISyntaxInspector
{
    public ImmutableArray<ISyntaxFilter> Filters { get; } = [TypeWithAttribute.Instance];

    public IImmutableSet<SyntaxKind> SupportedKinds { get; } =
    [
        SyntaxKind.ClassDeclaration,
        SyntaxKind.RecordDeclaration,
        SyntaxKind.InterfaceDeclaration,
        SyntaxKind.EnumDeclaration
    ];

    public bool TryHandle(
        GeneratorSyntaxContext context,
        [NotNullWhen(true)] out SyntaxInfo? syntaxInfo)
    {
        if (context.Node is BaseTypeDeclarationSyntax { AttributeLists.Count: > 0 } possibleType)
        {
            foreach (var attributeListSyntax in possibleType.AttributeLists)
            {
                foreach (var attributeSyntax in attributeListSyntax.Attributes)
                {
                    var symbol = context.SemanticModel.GetSymbolInfo(attributeSyntax).Symbol;

                    if (symbol is not IMethodSymbol attributeSymbol)
                    {
                        continue;
                    }

                    var attributeContainingTypeSymbol = attributeSymbol.ContainingType;
                    var fullName = attributeContainingTypeSymbol.ToDisplayString();

                    // We do a startWith to capture the generic and non-generic variants of
                    // the object type extension attribute.
                    if (fullName.StartsWith(ExtendObjectTypeAttribute, Ordinal)
                        && context.SemanticModel.GetDeclaredSymbol(possibleType) is { } typeExt)
                    {
                        syntaxInfo = new TypeExtensionInfo(
                            typeExt.ToDisplayString(),
                            possibleType.Modifiers.Any(t => t.IsKind(SyntaxKind.StaticKeyword)));
                        return true;
                    }

                    if (attributeContainingTypeSymbol.TypeArguments.Length == 0
                        && TypeAttributes.Contains(fullName)
                        && context.SemanticModel.GetDeclaredSymbol(possibleType) is { } type)
                    {
                        if (fullName.Equals(QueryTypeAttribute))
                        {
                            if (type.IsStatic && possibleType.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
                            {
                                syntaxInfo = null;
                                return false;
                            }

                            syntaxInfo = new TypeExtensionInfo(
                                type.ToDisplayString(),
                                possibleType.Modifiers.Any(t => t.IsKind(SyntaxKind.StaticKeyword)),
                                OperationType.Query);
                            return true;
                        }

                        if (fullName.Equals(MutationTypeAttribute))
                        {
                            if (type.IsStatic && possibleType.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
                            {
                                syntaxInfo = null;
                                return false;
                            }

                            syntaxInfo = new TypeExtensionInfo(
                                type.ToDisplayString(),
                                possibleType.Modifiers.Any(t => t.IsKind(SyntaxKind.StaticKeyword)),
                                OperationType.Mutation);
                            return true;
                        }

                        if (fullName.Equals(SubscriptionTypeAttribute))
                        {
                            if (type.IsStatic && possibleType.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
                            {
                                syntaxInfo = null;
                                return false;
                            }

                            syntaxInfo = new TypeExtensionInfo(
                                type.ToDisplayString(),
                                possibleType.Modifiers.Any(t => t.IsKind(SyntaxKind.StaticKeyword)),
                                OperationType.Subscription);
                            return true;
                        }

                        syntaxInfo = new TypeInfo(type.ToDisplayString());
                        return true;
                    }
                }
            }
        }

        syntaxInfo = null;
        return false;
    }
}
