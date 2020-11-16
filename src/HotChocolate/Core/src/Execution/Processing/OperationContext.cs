using System;
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
    }
}
