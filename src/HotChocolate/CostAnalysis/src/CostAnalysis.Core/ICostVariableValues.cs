using HotChocolate.Language;

namespace HotChocolate.CostAnalysis;

/// <summary>
/// Resolves coerced variable values for a <see cref="CostPlan"/> evaluation.
/// </summary>
public interface ICostVariableValues
{
    /// <summary>
    /// Attempts to resolve the coerced value of the variable named
    /// <paramref name="name"/>.
    /// </summary>
    /// <param name="name">
    /// The variable name, without the leading <c>$</c>.
    /// </param>
    /// <param name="value">
    /// The coerced value, or <see langword="null"/> when this method returns
    /// <see langword="false"/>. A <see cref="NullValueNode"/> represents an
    /// explicit <c>null</c>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when the variable is defined, including an
    /// operation-level default when no value was supplied; otherwise
    /// <see langword="false"/>.
    /// </returns>
    bool TryGetValue(string name, out IValueNode? value);
}
