using System;
using System.Collections.Generic;
using System.Linq;
using GraphQLParser;
using GraphQLParser.AST;
using Zeus.Types;

namespace Zeus.Execution
{
    public class Document
        : IDocument
    {
        private GraphQLDocument _document;

        private Document(GraphQLDocument document)
        {
            _document = document;
            Operations = document.Definitions.OfType<GraphQLOperationDefinition>().ToList();
            Fragments = document.Definitions.OfType<GraphQLFragmentDefinition>().ToDictionary(t => t.Name.Value);
        }

        public List<GraphQLOperationDefinition> Operations { get; }
        public Dictionary<string, GraphQLFragmentDefinition> Fragments { get; }

        public GraphQLOperationDefinition GetOperation(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                if (Operations.Count == 1)
                {
                    return Operations.First();
                }
                throw new Exception("TODO: Query Exception");
            }
            else
            {
                GraphQLOperationDefinition operation = Operations
                    .FirstOrDefault(t => t.Name.Value.Equals(name, StringComparison.Ordinal));
                if (operation == null)
                {
                    throw new Exception("TODO: Query Exception");
                }
                return operation;
            }
        }

        public static Document Parse(string document)
        {
            Source source = new Source(document);
            Parser parser = new Parser(new Lexer());
            return new Document(parser.Parse(source));
        }
    }
}