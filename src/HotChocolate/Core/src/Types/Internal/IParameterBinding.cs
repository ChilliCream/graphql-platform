using HotChocolate.Resolvers;

namespace HotChocolate.Internal;

/// <summary>
/// Defines a binding that resolves and injects parameter values at runtime.
/// </summary>
public interface IParameterBinding
{
    /// <summary>
    /// Gets a value indicating whether this binding produces pure values
    /// without side effects or external dependencies.
    /// </summary>
    bool IsPure { get; }

    /// <summary>
    /// Executes the binding to resolve the parameter value from the resolver context.
    /// </summary>
    /// <typeparam name="T">The expected type of the parameter value.</typeparam>
    /// <param name="context">The resolver context containing request state and services.</param>
    /// <returns>The resolved parameter value.</returns>
    T Execute<T>(IResolverContext context);
}
