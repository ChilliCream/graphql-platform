using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GraphQLParser;
using GraphQLParser.AST;
using Zeus.Types;

namespace Zeus.Execution
{
    public class RequestExecuter
    {
        //schema, document, operationName, variableValues, initialValue
        public async Task ExecuteAsync(Schema schema, Document document,
            string operationName, IDictionary<string, object> variables,
            object initialValue, CancellationToken cancellationToken)
        {
            GraphQLOperationDefinition operation = document.GetOperation(operationName);
            Queue<DocumentContext> queue = new Queue<DocumentContext>();
            EnqueueSelectionSet(queue, new TypeDeclaration("Query", false, TypeKind.Object), operation.SelectionSet);

            while (queue.Any())
            {
                DocumentContext current = queue.Dequeue();
                if (current.Node is GraphQLFieldSelection f)
                {
                    if (schema.TryGetObjectType(current.Type.Name, out var type)
                        && type.Fields.TryGetValue(f.Name.Value, out var field)
                        && schema.TryGetResolver(current.Type.Name, f.Name.Value, out var resolver))
                    {
                        current = current.SetTypeName(field.Type)
                            .AddResult(await resolver.ResolveAsync(current, cancellationToken));

                        foreach (ASTNode selection in f.SelectionSet.Selections)
                        {
                            queue.Enqueue(current.SetNode(selection));
                        }
                    }
                }
            }
        }

        private void EnqueueSelectionSet(Queue<DocumentContext> queue, TypeDeclaration type, GraphQLSelectionSet selectionSet)
        {
            foreach (ASTNode selection in selectionSet.Selections)
            {
                queue.Enqueue(new DocumentContext(selection, type));
            }
        }
    }

    public class DocumentContext
        : IResolverContext
    {
        public DocumentContext(ASTNode node, TypeDeclaration type)
            : this(node, type, ImmutableStack<object>.Empty)
        {

        }
        private DocumentContext(ASTNode node, TypeDeclaration type, IImmutableStack<object> path)
        {
            Node = node;
            Path = path;
            Type = type;
        }

        public IImmutableStack<object> Path { get; }
        public ASTNode Node { get; }
        public TypeDeclaration Type { get; }

        public T Argument<T>(string name)
        {
            throw new NotImplementedException();
        }

        public T Parent<T>()
        {
            return (T)Path.Peek();
        }

        public DocumentContext AddResult(object result)
        {
            return new DocumentContext(Node, Type, Path.Push(result));
        }

        public DocumentContext SetTypeName(TypeDeclaration type)
        {
            return new DocumentContext(Node, type, Path);
        }

        public DocumentContext SetNode(ASTNode node)
        {
            return new DocumentContext(node, Type, Path);
        }
    }

    public class Document
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