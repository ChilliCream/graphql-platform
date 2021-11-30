using System.Reflection;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Internal;

/// <summary>
/// The parameter context builder allows extensions to build context data on a field.
/// </summary>
public interface IParameterContextBuilder : IParameterHandler
{
    /// <summary>
    /// Builds the context data for the specified parameter on the specified fieldDescriptor.
    /// </summary>
    /// <param name="parameter">
    /// The resolver parameter.
    /// </param>
    /// <param name="fieldDescriptor">
    /// The field descriptor.
    /// </param>
    void BuildContextData(ParameterInfo parameter, ObjectFieldDescriptor fieldDescriptor);
}
