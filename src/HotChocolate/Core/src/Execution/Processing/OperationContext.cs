using System;
using HotChocolate.Execution.Processing.Tasks;
using HotChocolate.Execution.Properties;
using HotChocolate.Types;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Execution.Processing;

internal sealed partial class OperationContext : IOperationContext
{
    public IOperation Operation
    {
        get
        {
            AssertInitialized();
            return _operation;
        }
    }

    public object? RootValue
    {
        get
        {
            AssertInitialized();
            return _rootValue;
        }
    }

    public IVariableValueCollection Variables
    {
        get
        {
            AssertInitialized();
            return _variables;
        }
    }

    public long IncludeFlags { get; private set; }

    public IServiceProvider Services
    {
        get
        {
            AssertInitialized();
            return _services;
        }
    }

    public ResultBuilder Result
    {
        get
        {
            AssertInitialized();
            return _resultHelper;
        }
    }

    public IWorkScheduler Scheduler
    {
        get
        {
            AssertInitialized();
            return _workScheduler;
        }
    }

    public ObjectPool<ResolverTask> ResolverTasks
    {
        get
        {
            AssertInitialized();
            return _resolverTaskPool;
        }
    }

    public PathFactory PathFactory
    {
        get
        {
            AssertInitialized();
            return _pathFactory;
        }
    }

    public ISelectionSet CollectFields(ISelection selection, IObjectType objectType)
    {
        AssertInitialized();
        return Operation.GetSelectionSet(selection, objectType);
    }

    public void RegisterForCleanup(Action action)
    {
        if (action is null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        AssertInitialized();
        _cleanupActions.Add(action);
    }

    public T GetQueryRoot<T>()
    {
        AssertInitialized();

        var query = _resolveQueryRootValue();

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
