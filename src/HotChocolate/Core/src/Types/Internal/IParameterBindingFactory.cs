namespace HotChocolate.Internal;

/// <summary>
/// Defines a factory for creating parameter bindings based on parameter metadata.
/// </summary>
public interface IParameterBindingFactory
{
    /// <summary>
    /// Specifies that this handler is run after all non-default handlers.
    /// </summary>
    bool IsDefaultHandler { get; }

    /// <summary>
    /// Checks if this expression builder can handle the following parameter.
    /// </summary>
    /// <param name="parameter">
    /// The parameter that needs to be resolved.
    /// </param>
    /// <returns>
    /// <c>true</c> if the parameter can be handled by this expression builder;
    /// otherwise <c>false</c>.
    /// </returns>
    bool CanHandle(ParameterDescriptor parameter);

    /// <summary>
    /// Creates a parameter binding for the specified parameter.
    /// </summary>
    /// <param name="parameter">
    /// The parameter descriptor containing metadata about the parameter to bind.
    /// </param>
    /// <returns>
    /// An <see cref="IParameterBinding"/> that defines how to resolve and inject
    /// the parameter value at runtime.
    /// </returns>
    IParameterBinding Create(ParameterDescriptor parameter);
}
