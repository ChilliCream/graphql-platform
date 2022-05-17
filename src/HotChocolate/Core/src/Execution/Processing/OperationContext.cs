using System;
using HotChocolate.Execution.Processing.Plan;
using HotChocolate.Execution.Processing.Tasks;
using HotChocolate.Execution.Properties;
using HotChocolate.Language;
using HotChocolate.Types;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Execution.Processing;

internal sealed partial class OperationContext : IOperationContext
{
    public IPreparedOperation Operation
    {
        get
        {
            AssertInitialized();
            return _operation;
        }
    }

    public QueryPlan QueryPlan
    {
        get
        {
            AssertInitialized();
            return _queryPlan;
        }
        set
        {
            AssertInitialized();
            _queryPlan = value;
            _workScheduler.ResetStateMachine();
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

    public IServiceProvider Services
    {
        get
        {
            AssertInitialized();
            return _services;
        }
    }

    public IResultHelper Result
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

    public ISelectionSet CollectFields(
        SelectionSetNode selectionSet,
        ObjectType objectType)
    {
        AssertInitialized();
        return Operation.GetSelectionSet(selectionSet, objectType);
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
