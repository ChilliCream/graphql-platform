using HotChocolate.Types.Analyzers.Inspectors;
using Microsoft.CodeAnalysis;

namespace HotChocolate.Types.Analyzers.Generators;

/// <summary>
/// A syntax generator produces C# code from the consumed syntax infos.
/// </summary>
public interface ISyntaxGenerator
{
    /// <summary>
    /// Allows to create initial code like attributes.
    /// </summary>
    /// <param name="context"></param>
    void Initialize(IncrementalGeneratorPostInitializationContext context);

    /// <summary>
    /// Specifies if the given <paramref name="syntaxInfo"/> will be consumed by this generator.
    /// </summary>
    bool Consume(ISyntaxInfo syntaxInfo);

    /// <summary>
    /// Generates the C# source code for the consumed syntax infos.
    /// </summary>
    void Generate(
        SourceProductionContext context,
        Compilation compilation,
        ReadOnlySpan<ISyntaxInfo> consumed);
}
