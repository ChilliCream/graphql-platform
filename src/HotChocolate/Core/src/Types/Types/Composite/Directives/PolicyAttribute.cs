using System.Reflection;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types.Composite;

/// <summary>
/// <para>
/// Applies the @policy directive to the annotated type or field to restrict access with
/// a policy expression in disjunctive normal form.
/// </para>
/// <para>
/// Each constructor argument is one OR alternative, and whitespace separated names inside
/// one argument form an AND group. [Policy("isAdmin isFinance", "isOwner")] produces the
/// expression (isAdmin AND isFinance) OR isOwner. Policy names that contain whitespace
/// are not expressible with this attribute and require the descriptor or SDL form.
/// </para>
/// <para>
/// @policy(names: [["isAdmin", "isFinance"], ["isOwner"]])
/// </para>
/// </summary>
[AttributeUsage(
    AttributeTargets.Class
    | AttributeTargets.Struct
    | AttributeTargets.Interface
    | AttributeTargets.Method
    | AttributeTargets.Property,
    AllowMultiple = true)]
public sealed class PolicyAttribute : DescriptorAttribute
{
    private readonly string[][] _names;

    /// <summary>
    /// Initializes a new instance of the <see cref="PolicyAttribute"/> class.
    /// </summary>
    /// <param name="groups">
    /// The policy name groups. Each argument is one OR alternative, and whitespace
    /// separated names inside one argument form an AND group. Policy names that contain
    /// whitespace are not expressible with this attribute and require the descriptor
    /// or SDL form.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="groups"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// The <paramref name="groups"/> contains no group or a group is empty or whitespace.
    /// </exception>
    public PolicyAttribute(params string[] groups)
    {
        ArgumentNullException.ThrowIfNull(groups);

        if (groups.Length == 0)
        {
            throw new ArgumentException(
                "The policy expression must contain at least one policy name group.",
                nameof(groups));
        }

        var names = new string[groups.Length][];

        for (var i = 0; i < groups.Length; i++)
        {
            var group = groups[i]?.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);

            if (group is null || group.Length == 0)
            {
                throw new ArgumentException(
                    "A policy name group must contain at least one policy name.",
                    nameof(groups));
            }

            names[i] = group;
        }

        _names = names;
    }

    /// <summary>
    /// Gets or sets the consequence that applies when the policy expression denies access.
    /// </summary>
    public PolicyDenialBehavior OnDenied { get; set; } = PolicyDenialBehavior.Null;

    protected internal override void TryConfigure(
        IDescriptorContext context,
        IDescriptor descriptor,
        ICustomAttributeProvider? attributeProvider)
    {
        switch (descriptor)
        {
            case IObjectTypeDescriptor objectType:
                objectType.Policy(_names, OnDenied);
                break;

            case IInterfaceTypeDescriptor interfaceType:
                interfaceType.Policy(_names, OnDenied);
                break;

            case IObjectFieldDescriptor objectField:
                objectField.Policy(_names, OnDenied);
                break;

            case IInterfaceFieldDescriptor interfaceField:
                interfaceField.Policy(_names, OnDenied);
                break;

            default:
                throw new SchemaException(
                    SchemaErrorBuilder.New()
                        .SetMessage(
                            "Policy directive is only supported on object types, interface "
                            + "types, and field definitions.")
                        .SetExtension("member", attributeProvider)
                        .SetExtension("descriptor", descriptor)
                        .Build());
        }
    }
}
