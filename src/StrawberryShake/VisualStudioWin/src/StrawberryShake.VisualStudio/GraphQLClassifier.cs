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
                    case SyntaxClassificationKind.Description:
                        list.AddClassification(span, classification, _classifications.Comment);
                        break;

                    case SyntaxClassificationKind.Keyword:
                    case SyntaxClassificationKind.DirectiveKeyword:
                    case SyntaxClassificationKind.EnumKeyword:
                    case SyntaxClassificationKind.ExtendKeyword:
                    case SyntaxClassificationKind.FragmentKeyword:
                    case SyntaxClassificationKind.ImplementsKeyword:
                    case SyntaxClassificationKind.InputKeyword:
                    case SyntaxClassificationKind.InterfaceKeyword:
                    case SyntaxClassificationKind.OnKeyword:
                    case SyntaxClassificationKind.RepeatableKeyword:
                    case SyntaxClassificationKind.ScalarKeyword:
                    case SyntaxClassificationKind.SchemaKeyword:
                    case SyntaxClassificationKind.TypeKeyword:
                    case SyntaxClassificationKind.UnionKeyword:
                    case SyntaxClassificationKind.OperationKind:
                        list.AddClassification(span, classification, _classifications.Keyword);
                        break;

                    default:
                        list.AddClassification(span, classification, _classifications.Other);
                        break;
                }
            }

            return list;
        }
    }
}
