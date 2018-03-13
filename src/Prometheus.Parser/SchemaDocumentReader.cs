using System;
using System.Collections.Generic;
using System.Linq;
using Prometheus.Abstractions;

namespace Prometheus.Parser
{
    public class SchemaDocumentReader
        : ISchemDocumentReader
    {
        public SchemaDocument Read(IEnumerable<string> schemas)
        {
            if (schemas == null)
            {
                throw new System.ArgumentNullException(nameof(schemas));
            }

            SchemaSyntaxVisitor schemaSyntaxVisitor = new SchemaSyntaxVisitor();

            int count = 0;
            foreach (string schema in schemas)
            {
                count++;
                GraphQLParser.Source source = new GraphQLParser.Source(schema);
                GraphQLParser.Parser parser = new GraphQLParser.Parser(new GraphQLParser.Lexer());
                parser.Parse(source).Accept(schemaSyntaxVisitor);
            }

            if (schemaSyntaxVisitor.TypeDefinitions.Count == 0)
            {
                throw new ArgumentException("The specified schema is empty.", nameof(schemas));
            }
            else if (count == 1 || schemaSyntaxVisitor.TypeDefinitions.Count == 1)
            {
                return new SchemaDocument(schemaSyntaxVisitor.TypeDefinitions);
            }
            else
            {
                return new SchemaDocument(MergeTypeDefinitions(schemaSyntaxVisitor.TypeDefinitions));
            }
        }

        private IEnumerable<ITypeDefinition> MergeTypeDefinitions(IEnumerable<ITypeDefinition> typeDefinitions)
        {
            foreach (var kind in typeDefinitions.GroupBy(k => k.GetType()))
            {
                foreach (var name in kind.GroupBy(n => n.Name))
                {
                    ITypeDefinition first = name.First();
                    foreach (ITypeDefinition other in name.Skip(1))
                    {
                        first = first.Merge(other);
                    }
                    yield return first;
                }
            }
        }
    }
}
