using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Execution
{
    internal class ____ResolverContext
        : IResolverContext
    {
        private readonly IExecutionContext _executionContext;


        public QueryExecutionDiagnostics Diagnostics =>
            _executionContext.Diagnostics;


        public ISchema Schema => _executionContext.Schema;
        public DocumentNode Document => _executionContext.Operation.Document;
        public OperationDefinitionNode Operation =>
            _executionContext.Operation.Definition;


        public IDictionary<string, object> ContextData =>
            _executionContext.ContextData;
        public CancellationToken RequestAborted =>
            _executionContext.RequestAborted;


        public ObjectType ObjectType => throw new NotImplementedException();

        public ObjectField Field => throw new NotImplementedException();

        public FieldNode FieldSelection => throw new NotImplementedException();


        public IImmutableStack<object> Source { get; }

        public Path Path { get; }

        public IImmutableDictionary<string, object> ScopedContextData
        {
            get;
            set;
        }





        public T Argument<T>(NameString name)
        {
            throw new NotImplementedException();
        }

        public IReadOnlyCollection<FieldSelection> CollectFields(ObjectType typeContext)
        {
            throw new NotImplementedException();
        }

        public IReadOnlyCollection<FieldSelection> CollectFields(ObjectType typeContext, SelectionSetNode selectionSet)
        {
            throw new NotImplementedException();
        }

        public T CustomProperty<T>(string key)
        {
            throw new NotImplementedException();
        }

        public T Parent<T>()
        {
            throw new NotImplementedException();
        }

        public void ReportError(string errorMessage)
        {
            throw new NotImplementedException();
        }

        public void ReportError(IError error)
        {
            throw new NotImplementedException();
        }

        public T Resolver<T>()
        {
            throw new NotImplementedException();
        }

        public T Service<T>()
        {
            throw new NotImplementedException();
        }

        public object Service(Type service)
        {
            throw new NotImplementedException();
        }
    }
}
