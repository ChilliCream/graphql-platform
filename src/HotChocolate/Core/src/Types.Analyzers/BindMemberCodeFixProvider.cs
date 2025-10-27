using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HotChocolate.Types.Analyzers;

[Shared]
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(BindMemberCodeFixProvider))]
public sealed class BindMemberCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds { get; } = ["HC0094", "HC0095"];

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
        var attribute = node.AncestorsAndSelf().OfType<AttributeSyntax>().FirstOrDefault();
        if (attribute is null)
        {
            return;
        }

        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (semanticModel is null)
        {
            return;
        }

        // Get the containing class and find the ObjectType<T> type
        var classDeclaration = attribute.AncestorsAndSelf().OfType<ClassDeclarationSyntax>().FirstOrDefault();
        if (classDeclaration is null)
        {
            return;
        }

        var objectTypeArg = GetObjectTypeGenericArgument(semanticModel, classDeclaration);
        if (objectTypeArg is null)
        {
            return;
        }

        if (diagnostic.Id == "HC0094")
        {
            // Offer all valid members as code fixes
            RegisterMemberSuggestions(context, attribute, objectTypeArg, diagnostic);
        }
        else if (diagnostic.Id == "HC0095")
        {
            // For type mismatch, offer to change to the correct type
            RegisterTypeFix(context, attribute, objectTypeArg, diagnostic);
        }
    }

    private static void RegisterMemberSuggestions(
        CodeFixContext context,
        AttributeSyntax attribute,
        ITypeSymbol objectTypeArg,
        Diagnostic diagnostic)
    {
        // We get all public members (properties, fields, methods)
        // but limit them to 10 suggestions to avoid overwhelming the user.
        var members = objectTypeArg.GetMembers()
            .Where(m => m.DeclaredAccessibility == Accessibility.Public
                && (m is IPropertySymbol || m is IFieldSymbol))
            .OrderBy(m => m.Name)
            .Take(10);

        foreach (var member in members)
        {
            var title = $"Change to '{member.Name}'";

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: title,
                    createChangedDocument: c => ReplaceMemberNameAsync(
                        context.Document,
                        attribute,
                        member.Name,
                        c),
                    equivalenceKey: title),
                diagnostic);
        }
    }

    private static void RegisterTypeFix(
        CodeFixContext context,
        AttributeSyntax attribute,
        ITypeSymbol objectTypeArg,
        Diagnostic diagnostic)
    {
        var title = $"Change to '{objectTypeArg.Name}'";

        context.RegisterCodeFix(
            CodeAction.Create(
                title: title,
                createChangedDocument: c => ReplaceTypeInNameofAsync(
                    context.Document,
                    attribute,
                    objectTypeArg.Name,
                    c),
                equivalenceKey: title),
            diagnostic);
    }

    private static async Task<Document> ReplaceMemberNameAsync(
        Document document,
        AttributeSyntax attribute,
        string newMemberName,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null)
        {
            return document;
        }

        if (attribute.ArgumentList?.Arguments.Count == 0)
        {
            return document;
        }

        var argument = attribute.ArgumentList!.Arguments[0];
        AttributeArgumentSyntax newArgument;

        if (argument.Expression is InvocationExpressionSyntax invocation
            && invocation.Expression is IdentifierNameSyntax { Identifier.Text: "nameof" })
        {
            // It's a nameof expression - we need to update the member name inside
            if (invocation.ArgumentList.Arguments.Count > 0)
            {
                var nameofArg = invocation.ArgumentList.Arguments[0].Expression;

                if (nameofArg is MemberAccessExpressionSyntax memberAccess)
                {
                    // Type.Member format - replace the member name
                    var newMemberAccess = memberAccess.WithName(
                        SyntaxFactory.IdentifierName(newMemberName));

                    var newInvocation = invocation.WithArgumentList(
                        SyntaxFactory.ArgumentList(
                            SyntaxFactory.SingletonSeparatedList(
                                SyntaxFactory.Argument(newMemberAccess))));

                    newArgument = argument.WithExpression(newInvocation);
                }
                else
                {
                    // Simple identifier - replace it
                    var newIdentifier = SyntaxFactory.IdentifierName(newMemberName);
                    var newInvocation = invocation.WithArgumentList(
                        SyntaxFactory.ArgumentList(
                            SyntaxFactory.SingletonSeparatedList(
                                SyntaxFactory.Argument(newIdentifier))));

                    newArgument = argument.WithExpression(newInvocation);
                }
            }
            else
            {
                return document;
            }
        }
        else
        {
            // It's a string literal - replace the whole thing
            var newLiteral = SyntaxFactory.LiteralExpression(
                SyntaxKind.StringLiteralExpression,
                SyntaxFactory.Literal(newMemberName));

            newArgument = argument.WithExpression(newLiteral);
        }

        var newArgumentList = attribute.ArgumentList.WithArguments(
            attribute.ArgumentList.Arguments.Replace(argument, newArgument));

        var newAttribute = attribute.WithArgumentList(newArgumentList);
        var newRoot = root.ReplaceNode(attribute, newAttribute);

        return document.WithSyntaxRoot(newRoot);
    }

    private static async Task<Document> ReplaceTypeInNameofAsync(
        Document document,
        AttributeSyntax attribute,
        string newTypeName,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null)
        {
            return document;
        }

        if (attribute.ArgumentList?.Arguments.Count == 0)
        {
            return document;
        }

        var argument = attribute.ArgumentList!.Arguments[0];

        if (argument.Expression is not InvocationExpressionSyntax invocation
            || invocation.Expression is not IdentifierNameSyntax { Identifier.Text: "nameof" }
            || invocation.ArgumentList.Arguments.Count == 0)
        {
            return document;
        }

        var nameofArg = invocation.ArgumentList.Arguments[0].Expression;

        if (nameofArg is not MemberAccessExpressionSyntax memberAccess)
        {
            return document;
        }

        // Replace the type name in Type.Member
        var newTypeIdentifier = SyntaxFactory.IdentifierName(newTypeName);
        var newMemberAccess = memberAccess.WithExpression(newTypeIdentifier);

        var newInvocation = invocation.WithArgumentList(
            SyntaxFactory.ArgumentList(
                SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.Argument(newMemberAccess))));

        var newArgument = argument.WithExpression(newInvocation);
        var newArgumentList = attribute.ArgumentList.WithArguments(
            attribute.ArgumentList.Arguments.Replace(argument, newArgument));

        var newAttribute = attribute.WithArgumentList(newArgumentList);
        var newRoot = root.ReplaceNode(attribute, newAttribute);

        return document.WithSyntaxRoot(newRoot);
    }

    private static ITypeSymbol? GetObjectTypeGenericArgument(
        SemanticModel semanticModel,
        ClassDeclarationSyntax classDeclaration)
    {
        if (classDeclaration.AttributeLists.Count == 0)
        {
            return null;
        }

        foreach (var attributeList in classDeclaration.AttributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                var symbolInfo = semanticModel.GetSymbolInfo(attribute);
                if (symbolInfo.Symbol is not IMethodSymbol attributeSymbol)
                {
                    continue;
                }

                var attributeType = attributeSymbol.ContainingType;

                // Check if this is ObjectTypeAttribute (compare full name without generic args)
                if (attributeType is not INamedTypeSymbol namedAttributeType
                    || namedAttributeType.Name != "ObjectTypeAttribute"
                    || namedAttributeType.ContainingNamespace?.ToDisplayString() != "HotChocolate.Types")
                {
                    continue;
                }

                // Check if it's a generic attribute and extract the type argument
                if (namedAttributeType.IsGenericType && namedAttributeType.TypeArguments.Length == 1)
                {
                    return namedAttributeType.TypeArguments[0];
                }
            }
        }

        return null;
    }
}
