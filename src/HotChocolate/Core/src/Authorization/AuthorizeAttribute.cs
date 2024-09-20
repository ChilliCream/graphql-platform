using System.Reflection;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using static HotChocolate.WellKnownContextData;

namespace HotChocolate.Authorization;

/// <summary>
/// Applies the authorization directive to object types or object fields.
/// </summary>
[AttributeUsage(
    AttributeTargets.Class |
    AttributeTargets.Struct |
    AttributeTargets.Property |
    AttributeTargets.Method,
    AllowMultiple = true)]
public class AuthorizeAttribute : DescriptorAttribute
{
    /// <summary>
    /// Applies the authorization directive to object types or object fields.
    /// </summary>
    public AuthorizeAttribute()
    {
    }

    /// <summary>
    /// Applies the authorization directive with a specific policy to
    /// object types or object fields.
    /// </summary>
    public AuthorizeAttribute(string policy)
    {
        Policy = policy;
    }

    /// <summary>
    /// Applies the authorization directive with a specific policy to
    /// object types or object fields.
    /// </summary>
    public AuthorizeAttribute(string policy, ApplyPolicy apply)
    {
        Policy = policy;
        Apply = apply;
    }

    /// <summary>
    /// Applies the authorization directive with a specific policy to
    /// object types or object fields.
    /// </summary>
    public AuthorizeAttribute(ApplyPolicy apply)
    {
        Apply = apply;
    }

    /// <summary>
    /// Gets or sets the authorization policy.
    /// </summary>
    public string? Policy { get; set; }

    /// <summary>
    /// Gets or sets the authorization roles.
    /// </summary>
    public string[]? Roles { get; set; }

    /// <summary>
    /// Specifies when the authorization directive shall be applied.
    /// </summary>
    public ApplyPolicy Apply { get; set; } = ApplyPolicy.BeforeResolver;

    protected internal override void TryConfigure(
        IDescriptorContext context,
        IDescriptor descriptor,
        ICustomAttributeProvider element)
    {
        if (descriptor is IObjectTypeDescriptor type)
        {
            type.Directive(CreateDirective());
        }
        else if (descriptor is IObjectFieldDescriptor field)
        {
            if (Apply is ApplyPolicy.Validation)
            {
                field.Extend().Context.ContextData[AuthorizationRequestPolicy] = true;
            }

            field.Directive(CreateDirective());
        }
    }

    private AuthorizeDirective CreateDirective()
    {
        return new AuthorizeDirective(apply: Apply, policy: Policy, roles: Roles);
    }
}
