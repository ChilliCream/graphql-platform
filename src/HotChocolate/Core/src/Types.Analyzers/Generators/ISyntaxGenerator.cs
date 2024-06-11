using System.Collections.Immutable;
using HotChocolate.Types.Analyzers.Inspectors;
using Microsoft.CodeAnalysis;

namespace HotChocolate.Types.Analyzers.Generators;

public interface ISyntaxGenerator
{
    void Generate(
        SourceProductionContext context,
        Compilation compilation,
        ImmutableArray<ISyntaxInfo> syntaxInfos);
}
