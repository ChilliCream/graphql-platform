using System;
using System.Collections.Generic;
using System.Linq;

namespace StrawberryShake.VisualStudio.Language
{
    public class GraphQLDocument
    {
        private List<SyntaxClassification> _classifications = new List<SyntaxClassification>();

        private DocumentNode? _document;

        public GraphQLDocument()
        {
            
        }

        public void Parse(string sourceText)
        {
            try
            {
                var classifications = new List<SyntaxClassification>();
                var classifier = new StringGraphQLClassifier(
                    sourceText.AsSpan(),
                    classifications);
                classifier.Parse();
                _classifications = classifications;
            }
            catch
            {
            }
        }

        public IEnumerable<SyntaxClassification> GetSyntaxClassifications(int start, int length)
        {
            int end = start + length;

            foreach (SyntaxClassification classification in _classifications
                .Where(t => t.Start <= end && t.End >= start)
                .OrderBy(t => t.Start))
            {
                yield return classification;
            }
        }
    }

}
