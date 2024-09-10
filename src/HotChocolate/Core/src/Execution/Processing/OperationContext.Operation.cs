using HotChocolate.Execution.Properties;
using HotChocolate.Types;

namespace HotChocolate.Execution.Processing;

internal sealed partial class OperationContext
{
    /// <summary>
    /// Gets the schema on which the query is being executed.
    /// </summary>
    public ISchema Schema
    {
        get
        {
            AssertInitialized();
            return _schema;
        }
    }

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
