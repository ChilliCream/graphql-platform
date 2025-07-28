#nullable enable

using System.Reflection;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types.Composite;

/// <summary>
/// <para>
/// Applies the @require directive to this argument to express a data requirement.
/// The data requirement can only require data from other source schemas and cannot be used
/// to require data from the same source schema.
/// </para>
/// <para>
/// @require(field: "user.name")
/// </para>
/// <para>
/// <see href="https://graphql.github.io/composite-schemas-spec/draft/#sec--require"/>
/// </para>
/// </summary>
public class RequireAttribute : ArgumentDescriptorAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RequireAttribute"/> class.
    /// </summary>
    /// <param name="field">The field selection map.</param>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="field"/> is <c>null</c>.
    /// </exception>
    public RequireAttribute(string? field = null)
    {
        Field = field;
    }

    /// <summary>
    /// Gets the field selection map.
    /// </summary>
    public string? Field { get; }

    protected override void OnConfigure(
        IDescriptorContext context,
        IArgumentDescriptor descriptor,
        ParameterInfo parameter)
    {
        var field = Field;

        if (string.IsNullOrEmpty(field))
        {
            // todo : this works only for leafs, we need to validate this.
            field = context.Naming.GetArgumentName(parameter);
        }

        descriptor.Require(field);
    }
}
