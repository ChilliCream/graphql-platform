using System;
using HotChocolate.Execution.Processing.Tasks;
using HotChocolate.Execution.Properties;
using HotChocolate.Types;
using HotChocolate.Utilities;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Execution.Processing;

/// <summary>
/// The internal context of the execution engine.
/// </summary>
internal sealed partial class OperationContext : IOperationContext
{
    /// <summary>
    /// Gets the operation that is being executed.
    /// </summary>
    public IOperation Operation
    {
        get
        {
            AssertInitialized();
            return _operation;
        }
    }

    /// <summary>
    /// Gets the value representing the instance of the
    /// <see cref="IOperation.RootType" />
    /// </summary>
    public object? RootValue
    {
        get
        {
            AssertInitialized();
            return _rootValue;
        }
    }

    /// <summary>
    /// Gets the coerced variable values for the current operation.
    /// </summary>
    public IVariableValueCollection Variables
    {
        get
        {
            AssertInitialized();
            return _variables;
        }
    }

    /// <summary>
    /// Gets the include flags for the current request.
    /// </summary>
    public long IncludeFlags { get; private set; }

    /// <summary>
    /// Gets the request scoped services
    /// </summary>
    public IServiceProvider Services
    {
        get
        {
            AssertInitialized();
            return _services;
        }
    }

    /// <summary>
    /// Gets the type converter service.
    /// </summary>
    /// <value></value>
    public ITypeConverter Converter { get; }

    /// <summary>
    /// The result helper which provides utilities to build up the result.
    /// </summary>
    public ResultBuilder Result
    {
        get
        {
            AssertInitialized();
            return _resultHelper;
        }
    }

    /// <summary>
    /// The work scheduler organizes the processing of request tasks.
    /// </summary>
    public IWorkScheduler Scheduler
    {
        get
        {
            AssertInitialized();
            return _workScheduler;
        }
    }

    /// <summary>
    /// Gets the backlog of the task that shall be processed after
    /// all the main tasks have been executed.
    /// </summary>
    public IDeferredWorkScheduler DeferredScheduler
    {
        get
        {
            AssertInitialized();
            return _deferredWorkScheduler;
        }
    }

    /// <summary>
    /// Gets the resolver task pool.
    /// </summary>
    public ObjectPool<ResolverTask> ResolverTasks
    {
        get
        {
            AssertInitialized();
            return _resolverTaskPool;
        }
    }

    /// <summary>
    /// The factory for path <see cref="Path"/>.
    /// </summary>
    public PathFactory PathFactory
    {
        get
        {
            AssertInitialized();
            return _pathFactory;
        }
    }

    /// <summary>
    /// Get the fields for the specified selection set according to the execution plan.
    /// The selection set will show all possibilities and needs to be pre-processed.
    /// </summary>
    /// <param name="selection">
    /// The selection for which we want to get the compiled selection set.
    /// </param>
    /// <param name="typeContext">
    /// The type context.
    /// </param>
    /// <returns></returns>
    public ISelectionSet CollectFields(ISelection selection, IObjectType typeContext)
    {
        AssertInitialized();
        return Operation.GetSelectionSet(selection, typeContext);
    }

    /// <summary>
    /// Register cleanup tasks that will be executed after resolver execution is finished.
    /// </summary>
    /// <param name="action">
    /// Cleanup action.
    /// </param>
    public void RegisterForCleanup(Action action)
    {
        if (action is null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        AssertInitialized();
        _cleanupActions.Add(action);
    }

    /// <summary>
    /// Get the query root instance.
    /// </summary>
    /// <typeparam name="T">
    /// The type of the query root.
    /// </typeparam>
    /// <returns>
    /// Returns the query root instance.
    /// </returns>
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
