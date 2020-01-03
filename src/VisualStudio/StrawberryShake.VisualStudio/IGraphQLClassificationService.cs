using Microsoft.VisualStudio.Language.StandardClassification;
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

    public class GraphQLClassificationService : IGraphQLClassificationService
    {
        private readonly IStandardClassificationService _classifications;

        public GraphQLClassificationService(IStandardClassificationService classifications)
        {
            _classifications = classifications;
        }

        public IClassificationType Comment => _classifications.Comment;

        public IClassificationType Identifier => _classifications.FormalLanguage;

        public IClassificationType Keyword => _classifications.Keyword;

        public IClassificationType StringLiteral => _classifications.StringLiteral;

        public IClassificationType NumberLiteral => _classifications.NumberLiteral;

        public IClassificationType EnumLiteral => throw new System.NotImplementedException();

        public IClassificationType BooleanLiteral => throw new System.NotImplementedException();

        public IClassificationType WhiteSpace => _classifications.WhiteSpace;

        public IClassificationType Other => _classifications.Other;

        public IClassificationType SymbolDefinition => throw new System.NotImplementedException();

        public IClassificationType SymbolReference => throw new System.NotImplementedException();
    }
}
