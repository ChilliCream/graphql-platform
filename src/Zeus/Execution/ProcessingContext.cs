using System;
using System.Collections.Generic;
using GraphQLParser.AST;
using Zeus.Types;

namespace Zeus.Execution
{
    internal class QueryContext
    {
        public QueryContext(string typeName, GraphQLFieldSelection fieldSelection,
           IResolverContext resolverContext)
           : this(typeName, fieldSelection, resolverContext, new Dictionary<string, object>())
        {
        }

        public QueryContext(string typeName, GraphQLFieldSelection fieldSelection,
            IResolverContext resolverContext, IDictionary<string, object> result)
        {
            TypeName = typeName;
            FieldSelection = fieldSelection;
            ResolverContext = resolverContext;
            Response = result;
        }

        public string TypeName { get; }
        public GraphQLFieldSelection FieldSelection { get; }

        public IResolverContext ResolverContext { get; }
        public ResolverResult ResolverResult { get; set; }

        public IDictionary<string, object> Response { get; }
    }

    internal class FieldSelection
    {
        private readonly string _typeName;
        private readonly GraphQLFieldSelection _fieldSelection;

        public FieldSelection(string typeName, GraphQLFieldSelection fieldSelection)
        {
            TypeName = typeName;
            _fieldSelection = fieldSelection;
        }

        public string TypeName { get; }
        public string Name => _fieldSelection.Name.Value;
        public string Alias => _fieldSelection.Alias?.Value;
        public IReadOnlyDictionary<string, object> Arguments { get; }
    }


    public class QueueItem
    {
        public ResolverContext Context { get; set; }
        public GraphQLFieldSelection FieldSelection { get; set; }
        public string TypeName { get; set; }
        public ResolverResult Result { get; set; }
        public Dictionary<string, object> Map { get; set; }
    }

    public class ResolverResult
    {
        public ResolverResult(string typeName, FieldDeclaration field, object result)
        {
            TypeName = typeName ?? throw new ArgumentNullException(nameof(typeName));
            Field = field ?? throw new ArgumentNullException(nameof(field));
            Result = result;
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
}