using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Mocha.Analyzers;

/// <summary>
/// Defines a contract for inspecting syntax nodes and extracting mediator-related semantic information.
/// </summary>
public interface ISyntaxInspector
{
    /// <summary>
    /// Gets the syntax filters used to pre-screen candidate syntax nodes before inspection.
    /// </summary>
    ImmutableArray<ISyntaxFilter> Filters { get; }

    /// <summary>
    /// Gets the set of <see cref="SyntaxKind"/> values that this inspector can process.
    /// </summary>
    IImmutableSet<SyntaxKind> SupportedKinds { get; }

    /// <summary>
    /// Attempts to inspect the specified syntax node and produce a <see cref="SyntaxInfo"/> describing its mediator semantics.
    /// </summary>
    /// <param name="knownSymbols">The well-known Mocha type symbols resolved from the current compilation.</param>
    /// <param name="node">The syntax node to inspect.</param>
    /// <param name="semanticModel">The semantic model for the syntax tree containing <paramref name="node"/>.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <param name="syntaxInfo">
    /// When this method returns <see langword="true"/>, contains the extracted syntax information;
    /// otherwise, <see langword="null"/>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the inspector recognized the node and produced a result; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    bool TryHandle(
        KnownTypeSymbols knownSymbols,
        SyntaxNode node,
        SemanticModel semanticModel,
        CancellationToken cancellationToken,
        out SyntaxInfo? syntaxInfo);
}
