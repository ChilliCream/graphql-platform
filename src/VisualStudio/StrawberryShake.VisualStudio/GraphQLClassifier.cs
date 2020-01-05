using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using StrawberryShake.VisualStudio.Language;

namespace StrawberryShake.VisualStudio
{

    internal sealed class GraphQLClassifier : IClassifier
    {
        private readonly GraphQLDocument _document = new GraphQLDocument();
        private readonly ITextBuffer _buffer;
        private readonly IGraphQLClassificationService _classifications;

        public GraphQLClassifier(ITextBuffer buffer, IGraphQLClassificationService classifications)
        {
            _buffer = buffer;
            _classifications = classifications;
            _buffer.Changed += TextBufferChanged;
            _document.Parse(_buffer.CurrentSnapshot.GetText());
        }

        private void TextBufferChanged(object sender, TextContentChangedEventArgs e)
        {
            _document.Parse(_buffer.CurrentSnapshot.GetText());
        }

        public event EventHandler<ClassificationChangedEventArgs> ClassificationChanged;

        public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span)
        {
            var list = new List<ClassificationSpan>();

            foreach (SyntaxClassification classification in
                _document.GetSyntaxClassifications(span.Start.Position, span.Length))
            {
                switch (classification.Kind)
                {
                    case SyntaxClassificationKind.Comment:
                        list.AddClassification(span, classification, _classifications.Comment);
                        break;

                    default:
                        list.AddClassification(span, classification, _classifications.SymbolDefinition);
                        break;
                }
            }

            return list;
        }
    }

    internal static class ListExtensions
    {
        public static void AddClassification(
            this ICollection<ClassificationSpan> classifications,
            SnapshotSpan snapshotSpan,
            SyntaxClassification classification,
            IClassificationType type)
        {
            var span = new Span(classification.Start, classification.Length);
            classifications.Add(new ClassificationSpan(
                new SnapshotSpan(snapshotSpan.Snapshot, span),
                type));
        }
    }
}
