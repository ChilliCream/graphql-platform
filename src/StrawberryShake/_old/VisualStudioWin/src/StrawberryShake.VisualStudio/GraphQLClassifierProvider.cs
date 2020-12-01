using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace StrawberryShake.VisualStudio
{
    [Export(typeof(IClassifierProvider))]
    [Name(nameof(GraphQLClassifier))]
    [ContentType(GraphQL.ContentType)]
    internal sealed class GraphQLClassifierProvider : IClassifierProvider
    {
        [Import]
        private IClassificationTypeRegistryService _classificationRegistry;

        [Import]
        private IStandardClassificationService _classifications;

        public IClassifier GetClassifier(ITextBuffer textBuffer)
        {
            return new GraphQLClassifier(
                textBuffer,
                new GraphQLClassificationService(
                    _classifications,
                    _classificationRegistry));
        }
    }
}
