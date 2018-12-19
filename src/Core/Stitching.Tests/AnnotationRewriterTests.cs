
using System.IO;
using System.Text;
using System.Threading.Tasks;
using ChilliCream.Testing;
using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Stitching
{
    public class AnnotationRewriterTests
    {
        [Fact]
        public void AnnotateQueryFields()
        {
            // arrange
            string schemaSource = FileResource.Open("Stitching.graphql");
            string querySource = FileResource.Open("StitchingQuery.graphql");

            ISchema schema = Schema.Create(schemaSource,
                c =>
                {
                    c.RegisterDirective<SchemaDirectiveType>();
                    c.RegisterDirective<DelegateDirectiveType>();
                    c.Use(next => ctx => Task.FromResult<object>(null));
                });

            DocumentNode query = Parser.Default.Parse(querySource);

            var context = AnnotationContext.Create(schema);

            // act
            DocumentNode annotatedQuery =
                query.Rewrite<AnnotateQueryRewriter, AnnotationContext>(
                    context);

            // assert
            SerializeQuery(annotatedQuery).Snapshot();
        }


        private static string SerializeQuery(DocumentNode query)
        {
            var content = new StringBuilder();
            var documentWriter = new DocumentWriter(new StringWriter(content));
            var serializer = new QuerySyntaxSerializer();
            serializer.Visit(query, documentWriter);
            return content.ToString();
        }
    }
}
