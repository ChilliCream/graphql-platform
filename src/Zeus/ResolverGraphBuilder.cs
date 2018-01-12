namespace Zeus
{
    internal class ResolverGraphBuilder
    {
        void Foo()
        {
            string schema = File.ReadAllText(@"C:\Work\Configuration-Data\src\Query\Schema.gql");
            GraphQLParser.Source source = new GraphQLParser.Source("{ applications { name } }");
            GraphQLParser.Lexer lexer = new GraphQLParser.Lexer();
            GraphQLParser.Parser parser = new GraphQLParser.Parser(lexer);
            GraphQLParser.AST.GraphQLDocument document = parser.Parse(source);




        }
    }


}
