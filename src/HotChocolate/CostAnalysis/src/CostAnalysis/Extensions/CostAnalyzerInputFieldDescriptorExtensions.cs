// ReSharper disable CheckNamespace
using HotChocolate.CostAnalysis.Types;

namespace HotChocolate.Types;

/// <summary>
/// Provides extension methods to <see cref="IInputFieldDescriptor"/>.
/// </summary>
public static class CostAnalyzerInputFieldDescriptorExtensions
{
    /// <summary>
    /// Applies the <c>@cost</c> directive. The purpose of the <c>cost</c> directive is to define a
    /// <c>weight</c> for GraphQL types, fields, and arguments. Static analysis can use these
    /// weights when calculating the overall cost of a query or response.
    /// </summary>
    /// <param name="descriptor">
    /// The input field descriptor.
    /// </param>
    /// <param name="weight">
    /// The <c>weight</c> argument defines what value to add to the overall cost for every
    /// appearance, or possible appearance, of this input field.
    /// </param>
    /// <returns>
    /// Returns the input field descriptor for configuration chaining.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="descriptor"/> is <c>null</c>.
    /// </exception>
    public static IInputFieldDescriptor Cost(this IInputFieldDescriptor descriptor, double weight)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        return descriptor.Directive(new CostDirective(weight));
    }
}
