using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using HotChocolate.Language;
using HotChocolate.Types;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Execution
{
    internal partial class ResolverContext
        : IShared
    {
        private static readonly DefaultObjectPool<ResolverContext> _pool =
            new DefaultObjectPool<ResolverContext>(new ResolverContextPolicy(), 1024);
        private static readonly ConcurrentBag<ResolverContext> _pool2 =
            new ConcurrentBag<ResolverContext>();

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

            IsRoot = true;
            Path = Path.New(fieldSelection.ResponseName);
            Source = source;
            SourceObject = executionContext.Operation.RootValue;
            ScopedContextData = ImmutableDictionary<string, object>.Empty;

            _arguments = fieldSelection.CoerceArguments(
                executionContext.Variables,
                executionContext.Converter);

            string responseName = fieldSelection.ResponseName;
            PropagateNonNullViolation = () =>
            {
                serializedResult[responseName] = null;
            };
        }

        private void Initialize(
            FieldSelection fieldSelection,
            IImmutableStack<object> source,
            object sourceObject,
            ResolverContext sourceContext,
            IDictionary<string, object> serializedResult,
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

            bool isNonNullType = fieldSelection.Field.Type.IsNonNullType();
            string responseName = fieldSelection.ResponseName;
            Action parentPropagateNonNullViolation =
                sourceContext.PropagateNonNullViolation;

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
                serializedResult[responseName] = null;
            };
        }

        public static ResolverContext Rent(
            IExecutionContext executionContext,
            FieldSelection fieldSelection,
            IImmutableStack<object> source,
            IDictionary<string, object> serializedResult)
        {
            if (!_pool2.TryTake(out ResolverContext context))
            {
                context = new ResolverContext();
            }
            // var context = new ResolverContext(); // = _pool.Get();
            context.Initialize(
                executionContext,
                fieldSelection,
                source,
                serializedResult);
            executionContext.TrackContext(context);
            return context;
        }

        private static ResolverContext Rent(
            FieldSelection fieldSelection,
            IImmutableStack<object> source,
            object sourceObject,
            ResolverContext sourceContext,
            IDictionary<string, object> serializedResult,
            Path path,
            Action propagateNonNullViolation)
        {
            if (!_pool2.TryTake(out ResolverContext context))
            {
                context = new ResolverContext();
            }
            // var context = new ResolverContext();  // _pool.Get();
            context.Initialize(
                fieldSelection,
                source,
                sourceObject,
                sourceContext,
                serializedResult,
                path,
                propagateNonNullViolation);
            sourceContext._executionContext.TrackContext(context);
            return context;
        }

        public static void Return(ResolverContext rentedContext)
        {
            rentedContext.Clean();
            if (_pool2.Count < 1024)
            {
                _pool2.Add(rentedContext);
            }
            // _pool.Return(rentedContext);
        }
    }
}
