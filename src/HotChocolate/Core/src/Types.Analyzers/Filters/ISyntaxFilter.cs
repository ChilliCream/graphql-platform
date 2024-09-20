using Microsoft.CodeAnalysis;

namespace HotChocolate.Types.Analyzers.Filters;

public interface ISyntaxFilter
{
    bool IsMatch(SyntaxNode node);
}
