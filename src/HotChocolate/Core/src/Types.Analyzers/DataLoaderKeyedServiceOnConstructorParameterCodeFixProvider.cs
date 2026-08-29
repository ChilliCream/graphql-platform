using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HotChocolate.Types.Analyzers;

[Shared]
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DataLoaderKeyedServiceOnConstructorParameterCodeFixProvider))]
public sealed class DataLoaderKeyedServiceOnConstructorParameterCodeFixProvider : CodeFixProvider
{
    private const string Title = "Use FromKeyedServices";

    public override ImmutableArray<string> FixableDiagnosticIds { get; } =
        [ErrorCodes.Analyzers.DataLoaderKeyedServiceOnConstructorParameter];

    public override FixAllProvider GetFixAllProvider()
        => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);

        if (root is null || semanticModel is null)
        {
            return;
        }

        var attribute = root.FindNode(context.Diagnostics[0].Location.SourceSpan)
            .AncestorsAndSelf()
            .OfType<AttributeSyntax>()
            .FirstOrDefault();

        if (attribute is null || !CanReplace(attribute, semanticModel, context.CancellationToken))
        {
            return;
        }

        context.RegisterCodeFix(
            CodeAction.Create(
                Title,
                c => ReplaceAttributeAsync(context.Document, attribute, c),
                Title),
            context.Diagnostics);
    }

    private static bool CanReplace(
        AttributeSyntax attribute,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        if (semanticModel.GetSymbolInfo(attribute, cancellationToken).Symbol is not IMethodSymbol constructor
            || constructor.ContainingType.ToDisplayString() != WellKnownAttributes.ServiceAttribute
            || attribute.ArgumentList is not { Arguments.Count: > 0 } arguments)
        {
            return false;
        }

        return arguments.Arguments.Count == 1
            && (constructor.Parameters.Length == 1
                || arguments.Arguments[0].NameEquals?.Name.Identifier.ValueText == "Key");
    }

    private static async Task<Document> ReplaceAttributeAsync(
        Document document,
        AttributeSyntax attribute,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        if (root is null)
        {
            return document;
        }

        var replacement = attribute.WithName(
            SyntaxFactory.IdentifierName("FromKeyedServices").WithTriviaFrom(attribute.Name));
        if (replacement.ArgumentList is { Arguments.Count: 1 } argumentList

            && argumentList.Arguments[0] is var argument
            && argument.NameEquals?.Name.Identifier.ValueText == "Key")
        {
            replacement = replacement.WithArgumentList(
                replacement.ArgumentList.WithArguments(
                    SyntaxFactory.SingletonSeparatedList(argument.WithNameEquals(null))));
        }

        return document.WithSyntaxRoot(root.ReplaceNode(attribute, replacement));
    }
}
