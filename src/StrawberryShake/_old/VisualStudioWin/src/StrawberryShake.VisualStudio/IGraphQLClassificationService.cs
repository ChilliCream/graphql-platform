using Microsoft.VisualStudio.Text.Classification;

namespace StrawberryShake.VisualStudio
{
    public interface IGraphQLClassificationService
    {
        IClassificationType Comment { get; }

        IClassificationType Identifier { get; }

        IClassificationType Keyword { get; }

        IClassificationType StringLiteral { get; }

        IClassificationType NumberLiteral { get; }

        IClassificationType EnumLiteral { get; }

        IClassificationType BooleanLiteral { get; }

        IClassificationType WhiteSpace { get; }

        IClassificationType Other { get; }

        IClassificationType SymbolDefinition { get; }

        IClassificationType SymbolReference { get; }
    }
}
