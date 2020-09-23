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

        public IServiceProvider Services { get; private set; } = default!;

        public IResultHelper Result => _resultHelper;

        public IExecutionContext Execution => _executionContext;

        public ISelectionSet CollectFields(
            SelectionSetNode selectionSet,
            ObjectType objectType) =>
            Operation.GetSelectionSet(selectionSet, objectType);
    }
}