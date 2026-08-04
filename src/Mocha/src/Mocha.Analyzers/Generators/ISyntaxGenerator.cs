using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Mocha.Analyzers;

/// <summary>
/// Defines a contract for generating C# source code from collected <see cref="SyntaxInfo"/> entries.
/// </summary>
public interface ISyntaxGenerator
{
    /// <summary>
    /// Generates source code from the provided syntax information and adds it to the compilation output.
    /// </summary>
    /// <param name="context">The source production context for reporting diagnostics.</param>
    /// <param name="assemblyName">The name of the assembly being compiled.</param>
    /// <param name="moduleName">The module name derived from the assembly name, used to prefix generated type names.</param>
    /// <param name="syntaxInfos">The collected syntax information entries to generate code from.</param>
    /// <param name="addSource">A delegate that adds a generated source file with the specified hint name and content.</param>
    void Generate(
        SourceProductionContext context,
        string assemblyName,
        string moduleName,
        ImmutableArray<SyntaxInfo> syntaxInfos,
        Action<string, string> addSource);
}
