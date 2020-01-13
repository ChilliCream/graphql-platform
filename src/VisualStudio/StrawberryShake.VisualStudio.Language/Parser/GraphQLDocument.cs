using System;
using System.Collections.Generic;
using System.Linq;

namespace StrawberryShake.VisualStudio.Language
{
    public class GraphQLDocument
    {
        private readonly List<SyntaxClassification> _classifications =
            new List<SyntaxClassification>();

        private DocumentNode? _document;

        public GraphQLDocument()
        {
        }

        public void Parse(string sourceText)
        {
            var classifier = new StringGraphQLClassifier(
                sourceText.AsSpan(),
                _classifications);
            classifier.Parse();
        }

        public IEnumerable<SyntaxClassification> GetSyntaxClassifications(int start, int length)
        {
            int end = start + length;

            foreach (SyntaxClassification classification in _classifications
                .Where(t => t.Start >= start && t.Start < end)
                .OrderBy(t => t.Start))
            {
                yield return classification;
            }
        }
    }
}
