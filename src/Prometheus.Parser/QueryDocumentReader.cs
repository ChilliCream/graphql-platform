using Prometheus.Abstractions;

namespace Prometheus.Parser
{
    public class QueryDocumentReader
        : IQueryDocumentReader
    {
        public IQueryDocument Read(string query)
        {
            if (query == null)
            {
                throw new System.ArgumentNullException(nameof(query));
            }

            QuerySyntaxVisitor querySyntaxVisitor = new QuerySyntaxVisitor();
            
            GraphQLParser.Source source = new GraphQLParser.Source(query);
            GraphQLParser.Parser parser = new GraphQLParser.Parser(new GraphQLParser.Lexer());
            parser.Parse(source).Accept(querySyntaxVisitor);

            return new QueryDocument(querySyntaxVisitor.Definitions);
        }
    }
}