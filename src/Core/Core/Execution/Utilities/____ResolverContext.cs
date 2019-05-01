using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Utilities;

namespace HotChocolate.Execution
{
    internal partial class ____ResolverContext
        : IMiddlewareContext
    {
        private IExecutionContext _executionContext;
        private object _result;
        private IDictionary<string, object> _serializedResult;
        private FieldSelection _fieldSelection;
        private Dictionary<string, ArgumentValue> _arguments;

        public QueryExecutionDiagnostics Diagnostics =>
            _executionContext.Diagnostics;

        public ITypeConversion Converter =>
            _executionContext.Converter;

        public ISchema Schema => _executionContext.Schema;

        public DocumentNode Document => _executionContext.Operation.Document;

        public OperationDefinitionNode Operation =>
            _executionContext.Operation.Definition;

        public IDictionary<string, object> ContextData =>
            _executionContext.ContextData;
        public CancellationToken RequestAborted =>
            _executionContext.RequestAborted;


        public ObjectType ObjectType => _fieldSelection.Field.DeclaringType;

        public ObjectField Field => _fieldSelection.Field;

        public FieldNode FieldSelection => _fieldSelection.Selection;

        public string ResponseName => _fieldSelection.ResponseName;

        public IImmutableStack<object> Source { get; private set; }

        // TODO : is this the right name?
        public object SourceObject { get; private set; }

        public Path Path { get; private set; }

        public IImmutableDictionary<string, object> ScopedContextData
        {
            get;
            set;
        }

        public FieldDelegate Middleware { get; private set; }

        public Task Task { get; set; }

        public object Result
        {
            get => _result;
            set
            {
                if (value is IResolverResult r)
                {
                    if (r.IsError)
                    {
                        _result = ErrorBuilder.New()
                            .SetMessage(r.ErrorMessage)
                            .SetPath(Path)
                            .AddLocation(FieldSelection)
                            .Build();
                    }
                    else
                    {
                        _result = r.Value;
                    }
                }
                else
                {
                    _result = value;
                }
                IsResultModified = true;
            }
        }

        public bool IsResultModified { get; private set; }

        public Action PropagateNonNullViolation { get; private set; }

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

        public Task<T> ResolveAsync<T>()
        {
            throw new NotImplementedException();
        }
    }
}
