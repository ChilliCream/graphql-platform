using System;
using HotChocolate.Execution.Utilities;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Execution
{
    internal sealed partial class OperationContext : IOperationContext
    {

        public IPreparedOperation Operation { get; private set; } = default!;

        public object? RootValue { get; private set; }

        public IVariableValueCollection Variables { get; private set; } = default!;

        public IActivator Activator => throw new NotImplementedException();

        public IResultHelper Result => _resultHelper;

        public IExecutionContext Execution => _executionContext;

        public IPreparedSelectionList CollectFields(
            SelectionSetNode selectionSet, 
            ObjectType objectType) =>
            Operation.GetSelections(selectionSet, objectType);

        public IValueNode ReplaceVariables(IValueNode value, IType type)
        {
            throw new NotImplementedException();
        }
    }
}