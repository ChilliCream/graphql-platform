using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Execution
{
    // TODO : FIX the object
    internal partial class ResolverContext
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

            IsRoot = false;
            Path = null;
            Source = null;
            SourceObject = null;
            ScopedContextData = null;
            LocalContextData = null;

            Task = null;
            Result = null;
            IsResultModified = false;
            PropagateNonNullViolation = null;
        }

        private void Initialize(
            IExecutionContext executionContext,
            FieldSelection fieldSelection,
            IImmutableStack<object> source,
            FieldData serializedResult)
        {
            _executionContext = executionContext;
            _serializedResult = serializedResult;
            _fieldSelection = fieldSelection;

            IsRoot = true;
            Path = Path.New(fieldSelection.ResponseName);
            Source = source;
            SourceObject = executionContext.Operation.RootValue;
            ScopedContextData = ImmutableDictionary<string, object>.Empty;
            LocalContextData = ImmutableDictionary<string, object>.Empty;

            _arguments = fieldSelection.CoerceArguments(
                executionContext.Variables,
                executionContext.Converter);

            PropagateNonNullViolation = () =>
            {
                serializedResult.RemoveFieldValue(fieldSelection.ResponseIndex);
            };
        }

        private void Initialize(
            FieldSelection fieldSelection,
            IImmutableStack<object> source,
            object sourceObject,
            ResolverContext sourceContext,
            FieldData serializedResult,
            Path path,
            Action propagateNonNullViolation)
        {
            _executionContext = sourceContext._executionContext;
            _serializedResult = serializedResult;
            _fieldSelection = fieldSelection;

            _arguments = fieldSelection.CoerceArguments(
                sourceContext._executionContext.Variables,
                sourceContext._executionContext.Converter);

            Path = path;
            Source = source;
            SourceObject = sourceObject;
            ScopedContextData = sourceContext.ScopedContextData;
            LocalContextData = ImmutableDictionary<string, object>.Empty;

            bool isNonNullType = fieldSelection.Field.Type.IsNonNullType();
            Action parentPropagateNonNullViolation = sourceContext.PropagateNonNullViolation;

            PropagateNonNullViolation = () =>
            {
                if (isNonNullType)
                {
                    if (propagateNonNullViolation != null)
                    {
                        propagateNonNullViolation.Invoke();
                    }
                    else if (parentPropagateNonNullViolation != null)
                    {
                        parentPropagateNonNullViolation.Invoke();
                    }
                }
                serializedResult.RemoveFieldValue(fieldSelection.ResponseIndex);
            };
        }

        public static ResolverContext Rent(
            IExecutionContext executionContext,
            FieldSelection fieldSelection,
            IImmutableStack<object> source,
            FieldData serializedResult)
        {
            var context = new ResolverContext();
            context.Initialize(
                executionContext,
                fieldSelection,
                source,
                serializedResult);
            return context;
        }

        private static ResolverContext Rent(
            FieldSelection fieldSelection,
            IImmutableStack<object> source,
            object sourceObject,
            ResolverContext sourceContext,
            FieldData serializedResult,
            Path path,
            Action propagateNonNullViolation)
        {
            // var context = ObjectPools.ResolverContexts.Rent();
            var context = new ResolverContext();
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

        public static void Return(ResolverContext rentedContext)
        {
            //ObjectPools.ResolverContexts.Return(rentedContext);
        }

        public static void Return(IEnumerable<ResolverContext> rentedContexts)
        {
            foreach (ResolverContext rentedContext in rentedContexts)
            {
                if (rentedContext is null)
                {
                    break;
                }
                //ResolverContext.Return(rentedContext);
            }
        }

        public static void Return(ResolverContext[] rentedContexts)
        {
            for (int i = 0; i < rentedContexts.Length; i++)
            {
                if (rentedContexts[i] is null)
                {
                    break;
                }
                //ResolverContext.Return(rentedContexts[i]);
                rentedContexts[i] = null;
            }
        }
    }
}
