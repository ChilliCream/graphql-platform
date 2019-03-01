using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Properties;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Execution
{
    internal sealed class ResolverTask
    {
        private readonly IExecutionContext _executionContext;
        private readonly IDictionary<string, object> _result;
        private readonly ResolverTask _parent;
        private readonly Action _propagateNonNullViolation;

        public ResolverTask(
            IExecutionContext executionContext,
            FieldSelection fieldSelection,
            IImmutableStack<object> source,
            IDictionary<string, object> result)
        {
            _executionContext = executionContext;
            Source = source;
            ObjectType = fieldSelection.Field.DeclaringType;
            FieldSelection = fieldSelection;
            FieldType = fieldSelection.Field.Type;
            Path = Path.New(fieldSelection.ResponseName);
            _result = result;
            ScopedContextData = ImmutableDictionary<string, object>.Empty;

            ResolverContext = new ResolverContext(
                executionContext, this,
                executionContext.RequestAborted);

            FieldDelegate = executionContext.FieldHelper
                .CreateMiddleware(fieldSelection);
        }

        private ResolverTask(
            ResolverTask parent,
            FieldSelection fieldSelection,
            Path path,
            IImmutableStack<object> source,
            IDictionary<string, object> result,
            Action propagateNonNullViolation)
        {
            _parent = parent;
            _executionContext = parent._executionContext;
            Source = source;
            ObjectType = fieldSelection.Field.DeclaringType;
            FieldSelection = fieldSelection;
            FieldType = fieldSelection.Field.Type;
            Path = path;
            _result = result;
            ScopedContextData = parent.ScopedContextData;
            _propagateNonNullViolation = propagateNonNullViolation;

            ResolverContext = new ResolverContext(
                parent._executionContext, this,
                parent._executionContext.RequestAborted);

            FieldDelegate = parent._executionContext.FieldHelper
                .CreateMiddleware(fieldSelection);
        }

        public ResolverTask Branch(
            FieldSelection fieldSelection,
            Path path,
            IImmutableStack<object> source,
            IDictionary<string, object> result,
            Action propagateNonNullViolation)
        {
            return new ResolverTask(
                this,
                fieldSelection,
                path,
                source,
                result,
                propagateNonNullViolation);
        }

        public IImmutableStack<object> Source { get; }

        public ObjectType ObjectType { get; }

        public FieldSelection FieldSelection { get; }

        public IType FieldType { get; }

        public Path Path { get; }

        public IResolverContext ResolverContext { get; }

        public Task<object> Task { get; set; }

        public object ResolverResult { get; set; }

        public FieldDelegate FieldDelegate { get; }

        public IImmutableDictionary<string, object> ScopedContextData
        {
            get;
            set;
        }

        public QueryExecutionDiagnostics Diagnostics
        {
            get
            {
                return _executionContext.Diagnostics;
            }
        }

        public void PropagateNonNullViolation()
        {
            if (FieldSelection.Field.Type.IsNonNullType())
            {
                if (_propagateNonNullViolation != null)
                {
                    _propagateNonNullViolation.Invoke();
                }
                else if (_parent != null)
                {
                    _parent.PropagateNonNullViolation();
                }
            }

            SetResult(null);
        }

        public void SetResult(object value)
        {
            _result[FieldSelection.ResponseName] = value;
        }

        public void ReportError(IError error)
        {
            if (error == null)
            {
                throw new ArgumentNullException(nameof(error));
            }

            _executionContext.AddError(error);
        }

        public IError CreateError(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                throw new ArgumentException(
                    CoreResources.ResolverTask_ErrorMessageIsNull,
                    nameof(message));
            }

            return QueryError.CreateFieldError(
                message,
                Path,
                FieldSelection.Selection);
        }
    }
}
