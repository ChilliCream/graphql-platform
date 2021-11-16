using HotChocolate.Language;

namespace HotChocolate.Lodash
{
    public static class TestExtensions
    {
        public static AggregationJsonRewriter CreateRewriter(
            this DocumentNode documentNode,
            ISchema schema)
        {
            AggregationVisitorContext context = new(documentNode.Definitions[0], schema);
            AggregationSyntaxVisitor syntaxVisitor = new();
            syntaxVisitor.Visit(documentNode.Definitions[0], context);
            return context.CreateRewriter();
        }

    }
    public static class TestHelpers {}
}
