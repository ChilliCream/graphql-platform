using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using GreenDonut;
using HotChocolate.Execution.Utilities;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Utilities;

namespace HotChocolate.Execution
{
    internal sealed partial class OperationContext : IOperationContext
    {
        public IPreparedOperation Operation { get; private set; }

        public object? RootValue { get; private set; }

        public IVariableValueCollection Variables { get; private set; }

        public IQueryResultBuilder Result => throw new NotImplementedException();

        public IActivator Activator { get; }

        public ITypeConversion Converter { get; }

        public ITaskQueue TaskQueue { get; }

        public IBatchScheduler BatchScheduler { get; private set; }

        public bool IsCompleted { get; private set; }

        public IExecutionContext Execution => throw new NotImplementedException();

        public void AddError(IError error, FieldNode? selection = null)
        {
            throw new NotImplementedException();
        }

        public void AddErrors(IEnumerable<IError> errors, FieldNode? selection = null)
        {
            throw new NotImplementedException();
        }

        public IPreparedSelectionList CollectFields(
            SelectionSetNode selectionSet,
            ObjectType objectType)
        {
            throw new NotImplementedException();
        }

        public void EnqueueResolverTask(
            IPreparedSelection selection,
            int responseIndex,
            ResultMap resultMap,
            object? parent,
            Path path,
            IImmutableDictionary<string, object?> scopedContextData)
        {
            throw new NotImplementedException();
        }

        public ResultList RentResultList()
        {
            throw new NotImplementedException();
        }

        public ResultMap RentResultMap(int count)
        {
            throw new NotImplementedException();
        }

        public ResultMapList RentResultMapList()
        {
            throw new NotImplementedException();
        }

        public IValueNode ReplaceVariables(IValueNode value, IType type)
        {
            throw new NotImplementedException();
        }

        public Task WaitForEngine()
        {
            throw new NotImplementedException();
        }
    }
}