using System;
using System.Collections.Generic;

namespace Prometheus.Language
{
    public class Parser
        : IParser
    {
        public DocumentNode Parse(ILexer lexer, ISource source)
        {
            if (lexer == null)
            {
                throw new ArgumentNullException(nameof(lexer));
            }

            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            Token start = lexer.Read(source);
            if (start.Kind != TokenKind.StartOfFile)
            {
                throw new InvalidOperationException("The first token must be a start of file token.");
            }
            return ParseDocument(source, start);
        }

        private DocumentNode ParseDocument(ISource source, Token start)
        {
            List<IDefinitionNode> definitions = new List<IDefinitionNode>();
            Token current = start;
            
            while (current.Kind != TokenKind.EndOfFile)
            {
                current = current.Next;
                definitions.Add(ParseDefinition(source, current));
            }

            throw new InvalidOperationException();

        }

        private IDefinitionNode ParseDefinition(ISource source, Token token)
        {
            if (token.Kind == TokenKind.Name)
            {
                switch (token.Value)
                {
                    case "query":
                    case "mutation":
                    case "subscription":
                    case "fragment":
                        throw new InvalidOperationException();
                    // return parseExecutableDefinition(lexer);
                    case "schema":
                    case "scalar":
                    case "type":
                    case "interface":
                    case "union":
                    case "enum":
                    case "input":
                    case "extend":
                    case "directive":
                        throw new InvalidOperationException();

                        // Note: The schema definition language is an experimental addition.
                        // return parseTypeSystemDefinition(lexer);
                }
            }
            else if (token.Kind == TokenKind.LeftBrace)
            {
                throw new InvalidOperationException();
                //return parseExecutableDefinition(lexer);
            }
            else if (token.Kind == TokenKind.BlockString
                || token.Kind == TokenKind.String)
            {
                throw new InvalidOperationException();

                // Note: The schema definition language is an experimental addition.
                // return parseTypeSystemDefinition(lexer);
            }

            throw new InvalidOperationException();

            // throw unexpected(lexer);
        }
    }

}