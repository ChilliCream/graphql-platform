using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Snapshooter.Xunit;
using Xunit;

namespace StrawberryShake.VisualStudio.Language.Tests.Classification
{
    public class StringGraphQLClassification
    {
        [Fact]
        public void Document_With_One_Operation()
        {
            string query = "query foo { }";
            var classifications = new List<SyntaxClassification>();

            var classifier = new StringGraphQLClassifier(query.AsSpan(),classifications);
            classifier.Parse();

            classifications.MatchSnapshot();
        }

        [Fact]
        public void Document_With_Two_Operations()
        {
            string query = "query foo { } query bar { }";
            var classifications = new List<SyntaxClassification>();

            var classifier = new StringGraphQLClassifier(query.AsSpan(), classifications);
            classifier.Parse();

            classifications.MatchSnapshot();
        }

        [Fact]
        public void Document_With_Block_String()
        {
            string query = @"
                query foo { } 

                """"""
                a
                b
                """"""

                query foo { } 
            ";
            var classifications = new List<SyntaxClassification>();

            var classifier = new StringGraphQLClassifier(query.AsSpan(), classifications);
            classifier.Parse();

            classifications.MatchSnapshot();
        }
    }
}
