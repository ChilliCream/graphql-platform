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
        : IShared
    {
        public void Clean()
        {
            _executionContext = null;
            _serializedResult = null;
            _fieldSelection = null;
            _arguments = null;
            _cachedResolverResult = null;
            _hasCachedResolverResult = false;

            Path = null;
            Source = null;
            SourceObject = null;
            ScopedContextData = null;

            Middleware = null;
            Task = null;
            Result = null;
            IsResultModified = false;
            PropagateNonNullViolation = null;
        }

        private void Initialize(
            IExecutionContext executionContext,
            FieldSelection fieldSelection,
            IImmutableStack<object> source,
            IDictionary<string, object> serializedResult)
        {
            _executionContext = executionContext;
            _serializedResult = serializedResult;
            _fieldSelection = fieldSelection;

            Path = Path.New(fieldSelection.ResponseName); ;
            Source = source;
            SourceObject = executionContext.Operation.RootValue;
            ScopedContextData = ImmutableDictionary<string, object>.Empty;

            Middleware = executionContext.FieldHelper
                .CreateMiddleware(fieldSelection);

            _arguments = fieldSelection.CoerceArgumentValues(
                executionContext.Variables, Path);
        }

        private void Initialize(
            FieldSelection fieldSelection,
            Path path,
            IImmutableStack<object> source,
            object sourceObject,
            ____ResolverContext sourceContext,
            IDictionary<string, object> serializedResult,
            Action propagateNonNullViolation)
        {
            _executionContext = sourceContext._executionContext;
            _serializedResult = serializedResult;
            _fieldSelection = fieldSelection;

            _arguments = null;

            Path = null;
            Source = source;
            SourceObject = sourceObject;
            ScopedContextData = sourceContext.ScopedContextData;

            Middleware = null;
            Task = null;
            Result = null;
            IsResultModified = false;
            PropagateNonNullViolation = null;






            // -----

            ObjectType = fieldSelection.Field.DeclaringType;
            FieldSelection = fieldSelection;
            FieldType = fieldSelection.Field.Type;
            Path = path;
            _result = serializedResult;

            FieldDelegate = parent._executionContext.FieldHelper
                .CreateMiddleware(fieldSelection);
            _arguments = fieldSelection.CoerceArgumentValues(
                parent._executionContext.Variables, Path);

            bool isNonNullType = FieldSelection.Field.Type.IsNonNullType();
            string responseName = FieldSelection.ResponseName;
            Action parentPropagateNonNullViolation =
                parent.PropagateNonNullViolation;

            PropagateNonNullViolation = () =>
            {
                if (isNonNullType)
                {
                    if (PropagateNonNullViolation != null)
                    {
                        propagateNonNullViolation.Invoke();
                    }
                    else if (parentPropagateNonNullViolation != null)
                    {
                        parentPropagateNonNullViolation.Invoke();
                    }
                }
                serializedResult[responseName] = null;
            };
        }


        public static ____ResolverContext Rent(
            IExecutionContext executionContext,
            FieldSelection fieldSelection,
            IImmutableStack<object> source,
            IDictionary<string, object> serializedResult)
        {
            var context = new ____ResolverContext();
            context.Initialize(
                executionContext,
                fieldSelection,
                source,
                serializedResult);
            return context;
        }

        public static void Return(____ResolverContext rentedContext)
        {

        }
    }
}
