using System;
using System.Collections;
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
        public async Task<IDictionary<string, object>> ExecuteAsync(Schema schema, Document document,
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

            return null;
        }

        private async Task ExecuteLevelAsync(Schema schema, IDictionary<string, object> variables,
            IEnumerable<QueueItem> items, CancellationToken cancellationToken)
        {
            List<QueueItem> nextLevel = new List<QueueItem>();

            // resolve => this could be done in parallel
            foreach (QueueItem item in items)
            {
                item.Result = await ResolveAsync(schema,
                    item.TypeName, item.FieldSelection,
                    variables, cancellationToken);
            }

            // execute batches => this could  be done in parallel
            // ExecuteBatches

            // invoke func results and queue next level => sync
            foreach (QueueItem item in items)
            {
                item.Result.FinalizeResult();
                // validate result against schema


                if (item.Result.Field.Type.Kind == TypeKind.List)
                {
                    List<Dictionary<string, object>> list = new List<Dictionary<string, object>>();
                    item.Map[item.FieldSelection.Alias.Value] = list;

                    foreach (object o in (IEnumerable)item.Result)
                    {
                        Dictionary<string, object> map = new Dictionary<string, object>();
                        list.Add(map);

                        foreach (GraphQLFieldSelection f in item.FieldSelection.SelectionSet.Selections)
                        {
                            nextLevel.Add(new QueueItem
                            {
                                Context = new ResolverContext(item.Context.Path.Push(o)),
                                FieldSelection = f,
                                TypeName = item.Result.Field.Type.ElementType.Name,
                                Map = map
                            });
                        }
                    }
                }
                else if (item.Result.Field.Type.Kind == TypeKind.Object)
                {
                    Dictionary<string, object> map = new Dictionary<string, object>();
                    item.Map[item.FieldSelection.Alias.Value] = map;
                    foreach (GraphQLFieldSelection f in item.FieldSelection.SelectionSet.Selections)
                    {
                        nextLevel.Add(new QueueItem
                        {
                            Context = new ResolverContext(item.Context.Path.Push(item.Result.Result)),
                            FieldSelection = f,
                            TypeName = item.Result.Field.Type.ElementType.Name,
                            Map = map
                        });
                    }
                }
                else if (item.Result.Field.Type.Kind == TypeKind.Scalar)
                {
                    item.Map[item.FieldSelection.Alias.Value] = item.Result.Result;
                }
            }
        }

        private class QueueItem
        {
            public ResolverContext Context { get; set; }
            public GraphQLFieldSelection FieldSelection { get; set; }
            public string TypeName { get; set; }
            public ResolverResult Result { get; set; }
            public Dictionary<string, object> Map { get; set; }
        }

        private async Task<ResolverResult> ResolveAsync(Schema schema, string typeName,
            GraphQLFieldSelection fieldSelection, IDictionary<string, object> variables,
            CancellationToken cancellationToken)
        {
            if (schema.TryGetObjectType(typeName, out ObjectDeclaration type))
            {
                if (type.Fields.TryGetValue(fieldSelection.Name.Value, out FieldDeclaration field))
                {
                    if (schema.TryGetResolver(typeName, fieldSelection.Name.Value, out var resolver))
                    {
                        object result = await resolver.ResolveAsync(null, cancellationToken);
                        return new ResolverResult(typeName, field, result);
                    }
                }
            }

            throw new Exception("TODO: Error Handling");
        }

        private void EnqueueSelectionSet(Queue<DocumentContext> queue, TypeDeclaration type, GraphQLSelectionSet selectionSet)
        {
            foreach (ASTNode selection in selectionSet.Selections)
            {
                queue.Enqueue(new DocumentContext(selection, type));
            }
        }
    }

    public class ResolverResult
    {
        public ResolverResult(string typeName, FieldDeclaration field, object result)
        {
            TypeName = typeName ?? throw new ArgumentNullException(nameof(typeName));
            Field = field ?? throw new ArgumentNullException(nameof(field));
        }

        public string TypeName { get; }
        public FieldDeclaration Field { get; }
        public object Result { get; private set; }


        public void FinalizeResult()
        {
            if (Result is Func<object>)
            {
                Result = ((Func<object>)Result)();
            }
        }
    }

    public class ResolverResultNode
    {
        public ResolverResultNode(ResolverResult result)
        {
            Result = result;
        }

        public ResolverResult Result { get; }
        public ICollection<ResolverResultNode> Nodes { get; } = new List<ResolverResultNode>();
    }

    public class ResolverContext
        : IResolverContext
    {
        public ResolverContext(IImmutableStack<object> path)
        {
            Path = path;
        }

        public IImmutableStack<object> Path { get; }
        internal IDictionary<string, object> Values { get; set; }

        public T Argument<T>(string name)
        {
            throw new NotImplementedException();
        }

        public T Parent<T>() => (T)Path.Peek();
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