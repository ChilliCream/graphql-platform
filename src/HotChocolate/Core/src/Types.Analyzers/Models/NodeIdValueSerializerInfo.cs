using HotChocolate.Types.Analyzers.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HotChocolate.Types.Analyzers.Models;

public sealed class NodeIdValueSerializerInfo : SyntaxInfo
{
    public NodeIdValueSerializerInfo(
        InvocationExpressionSyntax invocationExpression,
        INamedTypeSymbol compositeId,
        (string FilePath, int LineNumber, int CharacterNumber) location)
    {
        InvocationExpression = invocationExpression
            ?? throw new ArgumentNullException(nameof(invocationExpression));
        CompositeId = compositeId
            ?? throw new ArgumentNullException(nameof(compositeId));
        Location = location;
        OrderByKey = compositeId.ToFullyQualified();
    }

    public InvocationExpressionSyntax InvocationExpression { get; }

    public INamedTypeSymbol CompositeId { get; }

    public (string FilePath, int LineNumber, int CharacterNumber) Location { get; }

    public override string OrderByKey { get; }

    public override bool Equals(SyntaxInfo? other)
    {
        if (other is not NodeIdValueSerializerInfo otherInfo)
        {
            return false;
        }

        return InvocationExpression.ToString() == otherInfo.InvocationExpression.ToString()
            && CompositeId.ToFullyQualified() == otherInfo.CompositeId.ToFullyQualified();
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(
            InvocationExpression.ToString(),
            CompositeId.ToFullyQualified());
    }
}
