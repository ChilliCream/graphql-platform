using HotChocolate.Types;

#nullable enable

namespace HotChocolate.Resolvers;

/// <summary>
/// Encapsulates all resolver-specific information about the execution of
/// an individual field selection.
/// </summary>
public interface IMiddlewareContext : IResolverContext
{
    /// <summary>
    /// Gets or sets the value type hint.
    /// </summary>
    IType? ValueType { get; set; }

    /// <summary>
    /// Gets or sets the result of the middleware.
    /// </summary>
    object? Result { get; set; }

    /// <summary>
    /// Defines if at least one middleware has modified the result.
    /// </summary>
    /// <value></value>
    bool IsResultModified { get; }

    /// <summary>
    /// Allows to modify some aspects of the overall operation result.
    /// </summary>
    IOperationResultBuilder OperationResult { get; }

    /// <summary>
    /// Executes the field resolver and returns its result.
    /// </summary>
    /// <typeparam name="T">
    /// The type to which the result shall be casted.
    /// </typeparam>
    /// <returns>
    /// Returns the resolved field value.
    /// </returns>
    ValueTask<T> ResolveAsync<T>();

    /// <summary>
    /// Register cleanup tasks that will be executed after resolver execution is finished.
    /// </summary>
    /// <param name="action">
    /// The cleanup action.
    /// </param>
    /// <param name="cleanAfter">
    /// Specifies when the cleanup task shall be applied.
    /// </param>
    void RegisterForCleanup(Func<ValueTask> action, CleanAfter cleanAfter = CleanAfter.Resolver);

    /// <summary>
    /// Replaces the argument values for the current field execution pipeline.
    /// </summary>
    /// <param name="newArgumentValues">
    /// The new argument values that shall replace the current argument values.
    /// </param>
    /// <returns>
    /// Returns the original argument values map so that a middleware is able to conserve them
    /// and restore the initial state of the context after it finished to execute.
    /// </returns>
    IReadOnlyDictionary<string, ArgumentValue> ReplaceArguments(
        IReadOnlyDictionary<string, ArgumentValue> newArgumentValues);

    /// <summary>
    /// Replaces the argument values for the current field execution pipeline.
    /// </summary>
    /// <param name="replace">
    /// A delegate that gets the current argument values and returns the new ones.
    /// </param>
    /// <returns>
    /// Returns the original argument values map so that a middleware is able to conserve them
    /// and restore the initial state of the context after it finished to execute.
    /// </returns>
    IReadOnlyDictionary<string, ArgumentValue> ReplaceArguments(ReplaceArguments replace);

    /// <summary>
    /// Replaces a single argument of the current middleware context.
    /// </summary>
    /// <param name="argumentName">
    /// The argument value that shall be replaced with <paramref name="newArgumentValue"/>.
    /// </param>
    /// <param name="newArgumentValue">
    /// The new argument value.
    /// </param>
    /// <returns>
    /// Returns the old argument value so that the middleware can restore
    /// the state after it is finished.
    /// </returns>
    ArgumentValue ReplaceArgument(string argumentName, ArgumentValue newArgumentValue);

    /// <summary>
    /// Clones the current middleware context.
    /// </summary>
    /// <returns>
    /// Returns the cloned middleware context.
    /// </returns>
    new IMiddlewareContext Clone();
}
