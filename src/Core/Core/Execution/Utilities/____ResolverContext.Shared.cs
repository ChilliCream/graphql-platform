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
            IImmutableStack<object> source,
            object sourceObject,
            ____ResolverContext sourceContext,
            IDictionary<string, object> serializedResult,
            Path path,
            Action propagateNonNullViolation)
        {
            _executionContext = sourceContext._executionContext;
            _serializedResult = serializedResult;
            _fieldSelection = fieldSelection;

            _arguments = fieldSelection.CoerceArgumentValues(
                sourceContext._executionContext.Variables, path);

            Path = path;
            Source = source;
            SourceObject = sourceObject;
            ScopedContextData = sourceContext.ScopedContextData;

            Middleware = sourceContext._executionContext.FieldHelper
                .CreateMiddleware(fieldSelection);

            bool isNonNullType = fieldSelection.Field.Type.IsNonNullType();
            string responseName = fieldSelection.ResponseName;
            Action parentPropagateNonNullViolation =
                sourceContext.PropagateNonNullViolation;

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

        private static ____ResolverContext Rent(
            FieldSelection fieldSelection,
            IImmutableStack<object> source,
            object sourceObject,
            ____ResolverContext sourceContext,
            IDictionary<string, object> serializedResult,
            Path path,
            Action propagateNonNullViolation)
        {
            var context = new ____ResolverContext();
            context.Initialize(
                fieldSelection,
                source,
                sourceObject,
                sourceContext,
                serializedResult,
                path,
                propagateNonNullViolation);
            return context;
        }

        public static void Return(____ResolverContext rentedContext)
        {

        }
    }
}
