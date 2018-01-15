using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using GraphQLParser;
using GraphQLParser.AST;
using GraphQLParser.Exceptions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Zeus
{
    internal class QuerySyntaxProcessor
    {
        private readonly IResolverCollection _resolvers;

        public QuerySyntaxProcessor(IResolverCollection resolvers)
        {
            _resolvers = resolvers;
        }

        public void Foo(string query)
        {
            try
            {
                Source source = new Source(query);
                Lexer lexer = new Lexer();
                Parser parser = new Parser(lexer);
                GraphQLDocument document = parser.Parse(source);
                if (document.Kind != ASTNodeKind.Document)
                {
                    throw new InvalidOperationException();
                }

                var x = new JsonSerializerSettings
                {
                    Formatting = Formatting.Indented,
                    NullValueHandling = NullValueHandling.Ignore,

                };
                x.Converters.Add(new StringEnumConverter());

                string doc = JsonConvert.SerializeObject(document, x);

                Queue<ASTNode> queue = new Queue<ASTNode>(document.Definitions);
                while (queue.Any())
                {
                    ASTNode node = queue.Dequeue();
                    switch (node.Kind)
                    {
                        case ASTNodeKind.NamedType:
                            break;
                        default:
                            break;

                    }

                }
            }
            catch (GraphQLSyntaxErrorException ex)
            {

            }
        }


    }

    public interface ISyntaxNodeHandler
    {
        bool CanHandle(ASTNodeKind kind);

        Task HandleAsync(SyntaxHandlerContext context);
    }



    public class SyntaxHandlerContext
    {
        public ImmutableStack<ASTNode> SyntaxPath { get; }
        public ImmutableStack<object> GraphPath { get; }

    }

    public static class ASTNodeExtensions
    {



        public static void Accept(this ASTNode node, SyntaxNodeVisitor visitor)
        {
            visitor.Visit(node);
        }


    }
}
