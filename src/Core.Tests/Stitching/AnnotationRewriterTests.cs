
using System.IO;
using System.Text;
using System.Threading.Tasks;
using ChilliCream.Testing;
using HotChocolate.Language;
using HotChocolate.Stitching;
using Xunit;

namespace HotChocolate.Execution
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

            var context = new AnnotationContext(schema);

            // act
            DocumentNode annotatedQuery =
                query.Rewrite<AnnotationsQueryRewriter, AnnotationContext>(
                    context);

            // assert
            SerializeQuery(annotatedQuery).Snapshot();
        }


        private static string SerializeQuery(DocumentNode query)
        {
            var content = new StringBuilder();
            var stringWriter = new StringWriter(content);
            var documentWriter = new DocumentWriter(stringWriter);
            var serializer = new QuerySyntaxSerializer();
            serializer.Visit(query, documentWriter);
            return content.ToString();
        }

    }
}
