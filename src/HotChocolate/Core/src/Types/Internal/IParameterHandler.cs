using System.Reflection;

#nullable enable

namespace HotChocolate.Internal;

/// <summary>
/// This base interface is used by the resolver compiler to determine
/// if a expression builder or context builder can be applied to a parameter.
/// </summary>
public interface IParameterHandler
{
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
    bool CanHandle(ParameterInfo parameter);
}
