using System.Reflection;
using HotChocolate.Types.Descriptors;

#nullable enable

namespace HotChocolate.Types;

/// <summary>
/// An attribute used to specify the default value for an argument, directive argument, or input
/// field. This value will be used if no value is supplied for the associated argument or field in a
/// query/mutation.
/// </summary>
/// <remarks>
/// This attribute can be applied to parameters, properties. It has an impact on the GraphQL schema
/// generation by assigning default values to arguments and fields.
/// </remarks>
/// <example>
/// Here is an example of how to use this attribute:
/// <code>
/// public class InputWithDefault
/// {
///     [DefaultValue("abc")]
///     public string? WithStringDefault { get; set; }
///
///     [DefaultValue(null)]
///     public string? WithNullDefault { get; set; }
///
///     [DefaultValue(FooEnum.Bar)]
///     public FooEnum Enum { get; set; }
///
///     public string? WithoutDefault { get; set; }
/// }
/// </code>
/// This results in the following GraphQL schema:
/// <code>
/// input InputWithDefaultInput {
///   withStringDefault: String = "abc"
///   withNullDefault: String
///   enum: FooEnum! = BAR
///   withoutDefault: String
/// }
/// </code>
/// In the schema, the `withStringDefault` field will default to "abc", and the `enum` field will
/// default to BAR if no value is provided.
/// </example>
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
public sealed class DefaultValueAttribute : DescriptorAttribute
{
    /// <summary>
    /// Creates a new instance of <see cref="DefaultValueAttribute"/>.
    /// </summary>
    /// <param name="value">
    /// The default value.
    /// </param>
    public DefaultValueAttribute(object? value)
    {
        Value = value;
    }

    /// <summary>
    /// The default value.
    /// </summary>
    public object? Value { get; }

    /// <inheritdoc />
    protected internal override void TryConfigure(
        IDescriptorContext context,
        IDescriptor descriptor,
        ICustomAttributeProvider element)
    {
        if (descriptor is IArgumentDescriptor arg)
        {
            arg.DefaultValue(Value);
        }

        if (descriptor is IDirectiveArgumentDescriptor darg)
        {
            darg.DefaultValue(Value);
        }

        if (descriptor is IInputFieldDescriptor field)
        {
            field.DefaultValue(Value);
        }
    }
}
