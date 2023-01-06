using System;
using System.Reflection;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using static HotChocolate.WellKnownContextData;

namespace HotChocolate.Authorization;

[AttributeUsage(
    AttributeTargets.Class |
    AttributeTargets.Struct |
    AttributeTargets.Property |
    AttributeTargets.Method,
    AllowMultiple = true)]
public class AuthorizeAttribute : DescriptorAttribute
{
    public AuthorizeAttribute()
    {
    }

    public AuthorizeAttribute(string policy)
    {
        Policy = policy;
    }

    public AuthorizeAttribute(string policy, ApplyPolicy apply)
    {
        Policy = policy;
        Apply = apply;
    }

    public string? Policy { get; set; }

    public string[]? Roles { get; set; }

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
        if (Policy is not null)
        {
            return new AuthorizeDirective(Policy, apply: Apply);
        }

        if (Roles is not null)
        {
            return new AuthorizeDirective(Roles, apply: Apply);
        }

        return new AuthorizeDirective(apply: Apply);
    }
}
