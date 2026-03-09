using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HotChocolate.Types.Analyzers;

[Shared]
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(WrongAuthorizationAttributeCodeFixProvider))]
public sealed class WrongAuthorizationAttributeCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds { get; } =
        [Errors.WrongAuthorizeAttribute.Id];

    public override FixAllProvider GetFixAllProvider()
        => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null)
        {
            return;
        }

        var diagnostic = context.Diagnostics[0];
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        // Find the attribute syntax
        var node = root.FindNode(diagnosticSpan);
        var attributeSyntax = node.AncestorsAndSelf().OfType<AttributeSyntax>().FirstOrDefault();
        if (attributeSyntax is null)
        {
            return;
        }

        // Get the semantic model
        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (semanticModel is null)
        {
            return;
        }

        var symbolInfo = semanticModel.GetSymbolInfo(attributeSyntax);
        if (symbolInfo.Symbol is not IMethodSymbol attributeSymbol)
        {
            return;
        }

        var attributeType = attributeSymbol.ContainingType;
        var attributeName = attributeType.Name;

        // Determine the correct HotChocolate attribute name
        var hotChocolateAttributeName = attributeName switch
        {
            "AuthorizeAttribute" => "Authorize",
            "AllowAnonymousAttribute" => "AllowAnonymous",
            _ => null
        };

        if (hotChocolateAttributeName is null)
        {
            return;
        }

        var title = $"Replace with 'HotChocolate.Authorization.{hotChocolateAttributeName}'";

        context.RegisterCodeFix(
            CodeAction.Create(
                title: title,
                createChangedDocument: c => FixAttributeAsync(
                    context.Document,
                    attributeSyntax,
                    hotChocolateAttributeName,
                    semanticModel,
                    c),
                equivalenceKey: title),
            diagnostic);
    }

    private static async Task<Document> FixAttributeAsync(
        Document document,
        AttributeSyntax attributeSyntax,
        string hotChocolateAttributeName,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null)
        {
            return document;
        }

        // Create the new fully qualified attribute name
        var newAttributeName = SyntaxFactory.ParseName($"HotChocolate.Authorization.{hotChocolateAttributeName}")
            .WithTriviaFrom(attributeSyntax.Name);

        // Transform the argument list to handle Roles parameter
        var newArgumentList = TransformArgumentList(attributeSyntax.ArgumentList, semanticModel);

        // Create the new attribute with the transformed arguments
        var newAttribute = attributeSyntax
            .WithName(newAttributeName)
            .WithArgumentList(newArgumentList);

        // Replace the attribute
        var newRoot = root.ReplaceNode(attributeSyntax, newAttribute);

        return document.WithSyntaxRoot(newRoot);
    }

    private static AttributeArgumentListSyntax? TransformArgumentList(
        AttributeArgumentListSyntax? argumentList,
        SemanticModel semanticModel)
    {
        if (argumentList is null)
        {
            return null;
        }

        var newArguments = new List<AttributeArgumentSyntax>();

        foreach (var argument in argumentList.Arguments)
        {
            // Check if this is the Roles argument
            if (argument.NameEquals?.Name.Identifier.Text == "Roles")
            {
                var transformedArgument = TransformRolesArgument(argument, semanticModel);
                if (transformedArgument is not null)
                {
                    newArguments.Add(transformedArgument);
                    continue;
                }
            }

            // Keep other arguments as-is
            newArguments.Add(argument);
        }

        return SyntaxFactory.AttributeArgumentList(
            SyntaxFactory.SeparatedList(newArguments))
            .WithTriviaFrom(argumentList);
    }

    private static AttributeArgumentSyntax? TransformRolesArgument(
        AttributeArgumentSyntax argument,
        SemanticModel semanticModel)
    {
        ExpressionSyntax? collectionExpression = null;

        switch (argument.Expression)
        {
            // Handle string literals: Roles = "Admin,User"
            case LiteralExpressionSyntax literalExpression when literalExpression.IsKind(SyntaxKind.StringLiteralExpression):
                var roles = ParseRolesFromString(literalExpression.Token.ValueText);
                if (roles.Length > 0)
                {
                    var collectionElements = roles.Select(role =>
                        SyntaxFactory.ExpressionElement(
                            SyntaxFactory.LiteralExpression(
                                SyntaxKind.StringLiteralExpression,
                                SyntaxFactory.Literal(role))))
                        .ToArray();

                    collectionExpression = SyntaxFactory.CollectionExpression(
                        SyntaxFactory.SeparatedList<CollectionElementSyntax>(collectionElements));
                }
                break;

            // Handle identifiers and member access: Roles = AdminRole or Roles = SomeClass.AdminRole
            case IdentifierNameSyntax or MemberAccessExpressionSyntax:
                // Check if it's a compile-time constant
                var constantValue = semanticModel.GetConstantValue(argument.Expression);
                if (constantValue is { HasValue: true, Value: string rolesString })
                {
                    // If the constant contains commas, we can't automatically transform it
                    // because we'd need to split it. We leave fixing it to the developer.
                    if (rolesString.Contains(','))
                    {
                        return argument;
                    }

                    // Single role - wrap the constant reference in a collection
                    collectionExpression = SyntaxFactory.CollectionExpression(
                        SyntaxFactory.SingletonSeparatedList<CollectionElementSyntax>(
                            SyntaxFactory.ExpressionElement(argument.Expression)));
                }
                else
                {
                    // Not a compile-time constant or can't resolve - wrap the reference in a collection
                    collectionExpression = SyntaxFactory.CollectionExpression(
                        SyntaxFactory.SingletonSeparatedList<CollectionElementSyntax>(
                            SyntaxFactory.ExpressionElement(argument.Expression)));
                }
                break;
        }

        if (collectionExpression is null)
        {
            return null;
        }

        return SyntaxFactory.AttributeArgument(
            argument.NameEquals,
            argument.NameColon,
            collectionExpression)
            .WithTriviaFrom(argument);
    }

    private static string[] ParseRolesFromString(string rolesString)
    {
        return rolesString.Split(',')
            .Select(r => r.Trim())
            .Where(r => !string.IsNullOrEmpty(r))
            .ToArray();
    }
}
