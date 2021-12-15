using System.Reflection;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Internal;

/// <summary>
/// The parameter configuration builder allows extensions to configure a field when a certain
/// parameter is detected on the field resolver.
/// </summary>
public interface IParameterFieldConfiguration : IParameterHandler
{
    /// <summary>
    /// Applies configuration to a field descriptor based on a resolver parameter.
    /// </summary>
    /// <param name="parameter">
    /// The resolver parameter.
    /// </param>
    /// <param name="descriptor">
    /// The field descriptor.
    /// </param>
    void ApplyConfiguration(ParameterInfo parameter, ObjectFieldDescriptor descriptor);
}
