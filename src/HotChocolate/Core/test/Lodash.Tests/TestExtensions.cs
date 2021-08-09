using HotChocolate.Language;

namespace HotChocolate.Lodash
{
    public static class TestExtensions
    {
        public static LodashJsonRewriter CreateRewriter(this DocumentNode documentNode)
        {
            LodashVisitorContext context = new(documentNode.Definitions[0]);
            LodashSyntaxVisitor syntaxVisitor = new();
            syntaxVisitor.Visit(documentNode.Definitions[0], context);
            return context.CreateRewriter();
        }
    }
}
