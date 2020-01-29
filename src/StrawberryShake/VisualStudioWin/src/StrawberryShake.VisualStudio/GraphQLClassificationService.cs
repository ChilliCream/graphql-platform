using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text.Classification;

namespace StrawberryShake.VisualStudio
{
    public class GraphQLClassificationService : IGraphQLClassificationService
    {
        private readonly IStandardClassificationService _classifications;

        public GraphQLClassificationService(
            IStandardClassificationService classifications,
            IClassificationTypeRegistryService classificationTypeRegistry)
        {
            _classifications = classifications;
            Keyword = classificationTypeRegistry.GetClassificationType("GraphQL.Keyword");
        }

        public IClassificationType Comment => _classifications.Comment;

        public IClassificationType Identifier => _classifications.FormalLanguage;

        public IClassificationType Keyword { get; }

        public IClassificationType StringLiteral => _classifications.StringLiteral;

        public IClassificationType NumberLiteral => _classifications.NumberLiteral;

        public IClassificationType EnumLiteral => _classifications.Literal;

        public IClassificationType BooleanLiteral => _classifications.Literal;

        public IClassificationType WhiteSpace => _classifications.WhiteSpace;

        public IClassificationType Other => _classifications.Other;

        public IClassificationType SymbolDefinition => _classifications.SymbolDefinition;

        public IClassificationType SymbolReference => _classifications.SymbolReference;
    }
}
