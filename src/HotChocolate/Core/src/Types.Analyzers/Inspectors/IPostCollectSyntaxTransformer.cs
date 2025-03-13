using System.Collections.Immutable;
using HotChocolate.Types.Analyzers.Models;
using Microsoft.CodeAnalysis;

namespace HotChocolate.Types.Analyzers.Inspectors;

/// <summary>
/// The post collect syntax transformer allows to create syntax infos based on the collected syntax infos.
/// </summary>
public interface IPostCollectSyntaxTransformer
{
    ImmutableArray<SyntaxInfo> Transform(
        Compilation compilation,
        ImmutableArray<SyntaxInfo> syntaxInfos);
}
