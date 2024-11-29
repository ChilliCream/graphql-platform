using System.Diagnostics.CodeAnalysis;
using HotChocolate.Types.Analyzers.Filters;
using HotChocolate.Types.Analyzers.Models;
using Microsoft.CodeAnalysis;

namespace HotChocolate.Types.Analyzers.Inspectors;

/// <summary>
/// The syntax inspector will analyze a syntax node and try to reason out the semantics in a
/// Hot Chocolate server context.
/// </summary>
public interface ISyntaxInspector
{
    /// <summary>
    /// Gets the filters that is used to determine in what kinds of syntax nodes the inspector is interested.
    /// </summary>
    IReadOnlyList<ISyntaxFilter> Filters { get; }

    /// <summary>
    /// <para>
    /// Inspects the current syntax node and if the current inspector can handle
    /// the syntax will produce a syntax info.
    /// </para>
    /// <para>The syntax info is used by a syntax generator to produce source code.</para>
    /// </summary>
    bool TryHandle(
        GeneratorSyntaxContext context,
        [NotNullWhen(true)] out SyntaxInfo? syntaxInfo);
}
