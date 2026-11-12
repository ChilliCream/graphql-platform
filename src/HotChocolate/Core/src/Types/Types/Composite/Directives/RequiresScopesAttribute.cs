using System.Reflection;
using HotChocolate.Types.Descriptors;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Composite;

/// <summary>
/// Applies one @requiresScopes group to the annotated member. Repeat the attribute to add further
/// alternative groups.
/// </summary>
[AttributeUsage(
    AttributeTargets.Class
    | AttributeTargets.Struct
    | AttributeTargets.Interface
    | AttributeTargets.Enum
    | AttributeTargets.Method
    | AttributeTargets.Property,
    AllowMultiple = true)]
public sealed class RequiresScopesAttribute : DescriptorAttribute
{
    /// <summary>
    /// Initializes a new instance of <see cref="RequiresScopesAttribute"/>.
    /// </summary>
    /// <param name="scopes">
    /// The scopes that are required together.
    /// </param>
    public RequiresScopesAttribute(params string[] scopes)
    {
        Scopes = scopes ?? throw new ArgumentNullException(nameof(scopes));
    }

    /// <summary>
    /// Gets the scopes of this group.
    /// </summary>
    public IReadOnlyList<string> Scopes { get; }

    protected internal override void TryConfigure(
        IDescriptorContext context,
        IDescriptor descriptor,
        ICustomAttributeProvider? attributeProvider)
    {
        var values = new string[Scopes.Count];

        for (var i = 0; i < values.Length; i++)
        {
            values[i] = Scopes[i];
        }

        switch (descriptor)
        {
            case IObjectTypeDescriptor desc:
                desc.RequiresScopes(values);
                break;

            case IObjectFieldDescriptor desc:
                desc.RequiresScopes(values);
                break;

            case IInterfaceTypeDescriptor desc:
                desc.RequiresScopes(values);
                break;

            case IInterfaceFieldDescriptor desc:
                desc.RequiresScopes(values);
                break;

            case IEnumTypeDescriptor desc:
                desc.RequiresScopes(values);
                break;

            case IScalarTypeDescriptor desc:
                desc.RequiresScopes(values);
                break;

            default:
                throw ThrowHelper.AuthorizationDirective_UnsupportedDescriptor(
                    DirectiveNames.RequiresScopes.Name,
                    attributeProvider,
                    descriptor);
        }
    }
}
