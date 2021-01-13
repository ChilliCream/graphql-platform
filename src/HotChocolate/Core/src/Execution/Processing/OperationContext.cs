using System;
using System.Security.Cryptography;
using HotChocolate.Execution.Properties;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Execution.Processing
{
    internal sealed partial class OperationContext : IOperationContext
    {
        public IPreparedOperation Operation
        {
            get
            {
                AssertNotPooled();
                return _operation;
            }
        }

        public object? RootValue
        {
            get
            {
                AssertNotPooled();
                return _rootValue;
            }
        }

        public IVariableValueCollection Variables
        {
            get
            {
                AssertNotPooled();
                return _variables;
            }
        }

        public IServiceProvider Services
        {
            get
            {
                AssertNotPooled();
                return _services;
            }
        }

        public IResultHelper Result
        {
            get
            {
                AssertNotPooled();
                return _resultHelper;
            }
        }

        public IExecutionContext Execution
        {
            get
            {
                AssertNotPooled();
                return _executionContext;
            }
        }

        public ISelectionSet CollectFields(
            SelectionSetNode selectionSet,
            ObjectType objectType)
        {
            AssertNotPooled();
            return Operation.GetSelectionSet(selectionSet, objectType);
        }

        public void RegisterForCleanup(Action action)
        {
            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            AssertNotPooled();
            _cleanupActions.Add(action);
        }

        public T GetQueryRoot<T>()
        {
            AssertNotPooled();

            object? query = _resolveQueryRootValue();

            if (query is null &&
                typeof(T) == typeof(object) &&
                new object() is T dummy)
            {
                return dummy;
            }

            if (query is T casted)
            {
                return casted;
            }

            throw new InvalidCastException(
                string.Format(
                    Resources.OperationContext_GetQueryRoot_InvalidCast,
                    typeof(T).FullName ?? typeof(T).Name));
        }
    }
}
