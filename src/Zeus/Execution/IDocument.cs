using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GraphQLParser;
using GraphQLParser.AST;

namespace Zeus.Execution
{
    public class Document
    {
        private GraphQLDocument _document;

        private Document(GraphQLDocument document)
        {

        }

        public async Task ExecuteAsync(string operationName, Schema schema, IDictionary<string, object> variables)
        {
            Queue<ASTNode> queue = new Queue<ASTNode>();
            queue.Enqueue(_document);

            while (queue.Any())
            {
                // ASTNode node in 
            }
        }

        public static Document Parse(string document)
        {
            Source source = new Source(document);
            Parser parser = new Parser(new Lexer());
            return new Document(parser.Parse(source));
        }
    }

    public class DocumentContext
    {
        public DocumentContext()
            : this(ImmutableStack<ASTNode>.Empty)
        {
        }

        private DocumentContext(IImmutableStack<ASTNode> path)
        {
            Path = path;
        }

        public IImmutableStack<ASTNode> Path { get; }

        public DocumentContext Create(ASTNode node)
        {
            return new DocumentContext(Path.Push(node));
        }
    }


    public class DocumentNodeProcessor
        : DocumentNodeWalker
    {
        
    }


    public class DocumentNodeWalker
        : SyntaxNodeVisitor<DocumentContext>
    {
        private readonly Queue<QueueItem> _queue = new Queue<QueueItem>();

        protected DocumentNodeWalker() { }

        public override void Visit(ASTNode node, DocumentContext context)
        {
            EnqueueVisit(node, context);
            while (_queue.Any())
            {
                QueueItem current = _queue.Dequeue();
                base.Visit(node, context.Create(node));
            }
        }

        private void EnqueueVisit(ASTNode node, DocumentContext context)
        {
            _queue.Enqueue(new QueueItem(context, node));
        }

        private void EnqueueVisit(IEnumerable<ASTNode> nodes, DocumentContext context)
        {
            if (nodes != null)
            {
                foreach (ASTNode node in nodes)
                {
                    EnqueueVisit(node, context);
                }
            }
        }

        protected override void VisitDocument(GraphQLDocument document, DocumentContext context)
        {
            EnqueueVisit(document.Definitions, context);
        }

        protected override void VisitOperationDefinition(GraphQLOperationDefinition operationDefinition, DocumentContext context)
        {
            EnqueueVisit(operationDefinition.SelectionSet, context);
        }

        protected override void VisitSelectionSet(GraphQLSelectionSet selectionSet, DocumentContext context)
        {
            EnqueueVisit(selectionSet.Selections, context);
        }

        protected override void VisitField(GraphQLFieldSelection field, DocumentContext context)
        {
            EnqueueVisit(field.SelectionSet, context);
        }

        private class QueueItem
        {
            public QueueItem(DocumentContext parentContext, ASTNode node)
            {
                ParentContext = parentContext;
                Node = node;
            }

            public DocumentContext ParentContext { get; }
            public ASTNode Node { get; }
        }
    }
}