using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HotChocolate.Types.Analyzers;

[Shared]
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(NodeResolverIdParameterCodeFixProvider))]
public sealed class NodeResolverIdParameterCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds { get; } = ["HC0104"];

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

        // Find the parameter
        var node = root.FindNode(diagnosticSpan);
        var parameter = node.AncestorsAndSelf().OfType<ParameterSyntax>().FirstOrDefault();
        if (parameter is null)
        {
            return;
        }

        // Find the method declaration
        var methodDeclaration = parameter.AncestorsAndSelf().OfType<MethodDeclarationSyntax>().FirstOrDefault();
        if (methodDeclaration is null)
        {
            return;
        }

        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (semanticModel is null)
        {
            return;
        }

        var parameters = methodDeclaration.ParameterList.Parameters;

        // Case 1: Check if there's an "id" parameter somewhere in the list
        for (var i = 0; i < parameters.Count; i++)
        {
            if (parameters[i].Identifier.Text == "id" && i != 0)
            {
                // Found "id" parameter not in first position - move it
                const string title = "Move 'id' parameter to first position";
                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: title,
                        createChangedDocument: c => MoveParameterToFirstAsync(
                            context.Document,
                            methodDeclaration,
                            i,
                            false,
                            c),
                        equivalenceKey: title),
                    diagnostic);
                return;
            }
        }

        // Case 2: Check if there's a primitive type parameter ending with "Id"
        for (var i = 0; i < parameters.Count; i++)
        {
            var param = parameters[i];
            var paramName = param.Identifier.Text;

            if (paramName.EndsWith("Id", StringComparison.Ordinal) && paramName.Length > 2)
            {
                // Check if it's a primitive type
                var typeInfo = semanticModel.GetTypeInfo(param.Type!);
                if (typeInfo.Type is not null && IsPrimitiveType(typeInfo.Type))
                {
                    var title = $"Move '{paramName}' to first position and rename to 'id'";
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            title: title,
                            createChangedDocument: c => MoveParameterToFirstAsync(
                                context.Document,
                                methodDeclaration,
                                i,
                                true,
                                c),
                            equivalenceKey: title),
                        diagnostic);
                    return;
                }
            }
        }

        // No code fix available for other cases
    }

    private static async Task<Document> MoveParameterToFirstAsync(
        Document document,
        MethodDeclarationSyntax methodDeclaration,
        int parameterIndex,
        bool renameToId,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null)
        {
            return document;
        }

        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        if (semanticModel is null)
        {
            return document;
        }

        var parameters = methodDeclaration.ParameterList.Parameters;
        var parameterToMove = parameters[parameterIndex];

        // If we need to rename, find all references and rename them
        var updatedMethod = methodDeclaration;
        if (renameToId)
        {
            var parameterSymbol = semanticModel.GetDeclaredSymbol(parameterToMove, cancellationToken);
            if (parameterSymbol is not null)
            {
                // Find all identifier nodes in the method that reference this parameter
                var identifierNodes = methodDeclaration.DescendantNodes()
                    .OfType<IdentifierNameSyntax>()
                    .Where(id =>
                    {
                        var symbolInfo = semanticModel.GetSymbolInfo(id, cancellationToken);
                        return SymbolEqualityComparer.Default.Equals(symbolInfo.Symbol, parameterSymbol);
                    })
                    .ToList();

                // Replace all references with "id"
                var replacements = identifierNodes.ToDictionary(
                    node => (SyntaxNode)node,
                    node => (SyntaxNode)SyntaxFactory.IdentifierName("id").WithTriviaFrom(node));

                updatedMethod = methodDeclaration.ReplaceNodes(replacements.Keys, (original, _) => replacements[original]);

                // Also rename the parameter itself
                var oldParameter = updatedMethod.ParameterList.Parameters[parameterIndex];
                var newParameter = oldParameter.WithIdentifier(
                    SyntaxFactory.Identifier("id").WithTriviaFrom(oldParameter.Identifier));
                updatedMethod = updatedMethod.ReplaceNode(oldParameter, newParameter);
            }
        }

        // Get the updated parameters after potential renaming
        parameters = updatedMethod.ParameterList.Parameters;
        parameterToMove = parameters[parameterIndex];

        // Create new parameter list with the parameter moved to first position
        // We need to preserve the formatting (trivia) from the original parameters
        var originalSeparators = updatedMethod.ParameterList.Parameters.GetSeparators().ToList();
        var newParametersAndSeparators = new List<SyntaxNodeOrToken>();

        // Build the new ordered list of parameters
        var orderedParams = new List<ParameterSyntax>
        {
            parameterToMove
        };
        for (var i = 0; i < parameters.Count; i++)
        {
            if (i != parameterIndex)
            {
                orderedParams.Add(parameters[i]);
            }
        }

        // Now construct the SeparatedList with proper separators
        for (var i = 0; i < orderedParams.Count; i++)
        {
            var param = orderedParams[i];

            // Transfer the leading trivia from the parameter at this position in the original list
            if (i < parameters.Count)
            {
                param = param.WithLeadingTrivia(parameters[i].GetLeadingTrivia());
            }

            newParametersAndSeparators.Add(param);

            // Add separator if not the last parameter
            if (i < orderedParams.Count - 1)
            {
                // Use the separator from the corresponding position in the original list
                if (i < originalSeparators.Count)
                {
                    newParametersAndSeparators.Add(originalSeparators[i]);
                }
                else
                {
                    // Fallback: create a new comma separator
                    newParametersAndSeparators.Add(SyntaxFactory.Token(SyntaxKind.CommaToken));
                }
            }
        }

        var newParameterList = updatedMethod.ParameterList.WithParameters(
            SyntaxFactory.SeparatedList<ParameterSyntax>(newParametersAndSeparators));

        var finalMethod = updatedMethod.WithParameterList(newParameterList);
        var newRoot = root.ReplaceNode(methodDeclaration, finalMethod);

        return document.WithSyntaxRoot(newRoot);
    }

    private static bool IsPrimitiveType(ITypeSymbol type)
    {
        if (type.SpecialType != SpecialType.None)
        {
            return type.SpecialType is SpecialType.System_Boolean
                or SpecialType.System_Byte
                or SpecialType.System_SByte
                or SpecialType.System_Int16
                or SpecialType.System_UInt16
                or SpecialType.System_Int32
                or SpecialType.System_UInt32
                or SpecialType.System_Int64
                or SpecialType.System_UInt64
                or SpecialType.System_Decimal
                or SpecialType.System_Single
                or SpecialType.System_Double
                or SpecialType.System_String
                or SpecialType.System_Char
                or SpecialType.System_DateTime;
        }

        // Check for Guid
        if (type.Name == "Guid" && type.ContainingNamespace?.ToDisplayString() == "System")
        {
            return true;
        }

        return false;
    }
}
