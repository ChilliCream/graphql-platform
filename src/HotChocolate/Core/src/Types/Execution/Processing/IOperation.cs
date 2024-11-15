#nullable enable

using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Execution.Processing;

/// <summary>
/// Represents a compiled GraphQL operation.
/// </summary>
public interface IOperation : IHasReadOnlyContextData, IEnumerable<ISelectionSet>
{
    /// <summary>
    /// Gets the internal unique identifier for this operation.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the parsed query document that contains the
    /// operation-<see cref="Definition" />.
    /// </summary>
    DocumentNode Document { get; }

    /// <summary>
    /// Gets the syntax node representing the operation definition.
    /// </summary>
    OperationDefinitionNode Definition { get; }

    /// <summary>
    /// Gets the root type on which the operation is executed.
    /// </summary>
    ObjectType RootType { get; }

    /// <summary>
    /// Gets the name of the operation.
    /// </summary>
    string? Name { get; }

    /// <summary>
    /// Gets the operation type (Query, Mutation, Subscription).
    /// </summary>
    OperationType Type { get; }

    /// <summary>
    /// Gets the prepared root selections for this operation.
    /// </summary>
    /// <returns>
    /// Returns the prepared root selections for this operation.
    /// </returns>
    ISelectionSet RootSelectionSet { get; }

    /// <summary>
    /// Gets all selection variants of this operation.
    /// </summary>
    IReadOnlyList<ISelectionVariants> SelectionVariants { get; }

    /// <summary>
    /// Defines if this operation has deferred fragments or streams.
    /// </summary>
    bool HasIncrementalParts { get; }

    /// <summary>
    /// Gets the schema for which this operation is compiled.
    /// </summary>
    ISchema Schema { get; }

    /// <summary>
    /// Gets the selection set for the specified <paramref name="selection"/> and
    /// <paramref name="typeContext"/>.
    /// </summary>
    /// <param name="selection">
    /// The selection set for which the selection set shall be resolved.
    /// </param>
    /// <param name="typeContext">
    /// The result type context.
    /// </param>
    /// <returns>
    /// Returns the selection set for the specified <paramref name="selection"/> and
    /// <paramref name="typeContext"/>.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// The specified <paramref name="selection"/> has no selection set.
    /// </exception>
    ISelectionSet GetSelectionSet(ISelection selection, IObjectType typeContext);

    /// <summary>
    /// Gets the possible return types for the <paramref name="selection"/>.
    /// </summary>
    /// <param name="selection">
    /// The selection for which the possible result types shall be returned.
    /// </param>
    /// <returns>
    /// Returns the possible return types for the specified <paramref name="selection"/>.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// The specified <paramref name="selection"/> has no selection set.
    /// </exception>
    IEnumerable<IObjectType> GetPossibleTypes(ISelection selection);

    /// <summary>
    /// Creates the include flags for the specified variable values.
    /// </summary>
    /// <param name="variables">
    /// The variable values.
    /// </param>
    /// <returns>
    /// Returns the include flags for the specified variable values.
    /// </returns>
    long CreateIncludeFlags(IVariableValueCollection variables);

    bool TryGetState<TState>(out TState? state);

    bool TryGetState<TState>(string key, out TState? state);

    /// <summary>
    /// Gets or adds state to this operation.
    /// </summary>
    /// <typeparam name="TState">
    /// The type of the state.
    /// </typeparam>
    /// <param name="createState">
    /// The factory that creates the state if it does not exist.
    /// </param>
    /// <returns>
    /// Returns the state.
    /// </returns>
    TState GetOrAddState<TState>(
        Func<TState> createState);

    /// <summary>
    /// Gets or adds state to this operation.
    /// </summary>
    /// <typeparam name="TState">
    /// The type of the state.
    /// </typeparam>
    /// <typeparam name="TContext">
    /// The type of the context.
    /// </typeparam>
    /// <param name="createState">
    /// The factory that creates the state if it does not exist.
    /// </param>
    /// <param name="context">
    /// The context that is passed to the factory.
    /// </param>
    /// <returns>
    /// Returns the state.
    /// </returns>
    TState GetOrAddState<TState, TContext>(
        Func<TContext, TState> createState,
        TContext context);

    /// <summary>
    /// Gets or adds state to this operation.
    /// </summary>
    /// <typeparam name="TState">
    /// The type of the state.
    /// </typeparam>
    /// <typeparam name="TContext">
    /// The type of the context.
    /// </typeparam>
    /// <param name="key">
    /// The key of the state.
    /// </param>
    /// <param name="createState">
    /// The factory that creates the state if it does not exist.
    /// </param>
    /// <returns>
    /// Returns the state.
    /// </returns>
    TState GetOrAddState<TState, TContext>(
        string key,
        Func<string, TState> createState);

    /// <summary>
    /// Gets or adds state to this operation.
    /// </summary>
    /// <typeparam name="TState">
    /// The type of the state.
    /// </typeparam>
    /// <typeparam name="TContext">
    /// The type of the context.
    /// </typeparam>
    /// <param name="key">
    /// The key of the state.
    /// </param>
    /// <param name="createState">
    /// The factory that creates the state if it does not exist.
    /// </param>
    /// <param name="context">
    /// The context that is passed to the factory.
    /// </param>
    /// <returns>
    /// Returns the state.
    /// </returns>
    TState GetOrAddState<TState, TContext>(
        string key,
        Func<string, TContext, TState> createState,
        TContext context);
}
