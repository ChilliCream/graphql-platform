#nullable enable
using System.Reflection;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types;

/// <summary>
/// An attribute used to specify the default value in GraphQL syntax for an argument, directive
/// argument, or input field. This value will be parsed and used if no value is supplied for the
/// associated argument or field in a query/mutation.
/// </summary>
/// <remarks>
/// This attribute can be applied to parameters and properties. It differs from
/// <see cref="DefaultValueAttribute"/> in that it accepts a GraphQL syntax string, which allows
/// more complex default values.
/// </remarks>
/// <example>
/// Here is an example of how to use this attribute:
/// <code>
/// public class InputWithComplexDefault
/// {
///     [DefaultValueSyntax("[ { user:  { enabled: true }}]")]
///     public List&lt;User>? WithComplexDefault { get; set; }
///
///     public string? WithoutDefault { get; set; }
/// }
/// </code>
/// In the schema, the `WithComplexDefault` field will default to a list with a single user object
/// having `enabled` field set to true, if no value is provided.
/// </example>
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
public sealed class DefaultValueSyntaxAttribute : DescriptorAttribute
{
    /// <summary>
    /// Creates a new instance of <see cref="DefaultValueSyntaxAttribute"/>.
    /// </summary>
    public DefaultValueSyntaxAttribute(string? syntax)
    {
        Syntax = syntax;
    }

    /// <summary>
    /// The GraphQL syntax of the default value.
    /// </summary>
    public string? Syntax { get; }

    /// <inheritdoc />
    protected internal override void TryConfigure(
        IDescriptorContext context,
        IDescriptor descriptor,
        ICustomAttributeProvider element)
    {
        if (descriptor is IArgumentDescriptor arg)
        {
            arg.DefaultValueSyntax(Syntax);
        }

        if (descriptor is IDirectiveArgumentDescriptor darg)
        {
            darg.DefaultValueSyntax(Syntax);
        }

        if (descriptor is IInputFieldDescriptor field)
        {
            field.DefaultValueSyntax(Syntax);
        }
    }
}
