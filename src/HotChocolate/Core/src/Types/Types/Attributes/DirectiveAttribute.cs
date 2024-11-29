using System.Reflection;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types;

/// <summary>
/// This abstract base class can be used to create directive attributes and
/// reduces the amount of boilerplate code.
/// </summary>
/// <typeparam name="TDirective">
/// The directive type.
/// </typeparam>
public abstract class DirectiveAttribute<TDirective> : DescriptorAttribute where TDirective : class
{
    private readonly TDirective _directive;

    /// <summary>
    /// Initializes a new instance of <see cref="DirectiveAttribute{TDirective}"/>.
    /// </summary>
    /// <param name="directive">
    /// The directive instance that shall be added to the type system member.
    /// </param>
    protected DirectiveAttribute(TDirective directive)
    {
        _directive = directive ?? throw new ArgumentNullException(nameof(directive));
    }

    protected internal sealed override void TryConfigure(
        IDescriptorContext context,
        IDescriptor descriptor,
        ICustomAttributeProvider element)
    {
        switch (descriptor)
        {
            case ArgumentDescriptor desc:
                desc.Directive(_directive);
                break;

            case DirectiveArgumentDescriptor desc:
                desc.Directive(_directive);
                break;

            case EnumTypeDescriptor desc:
                desc.Directive(_directive);
                break;

            case EnumValueDescriptor desc:
                desc.Directive(_directive);
                break;

            case InputFieldDescriptor desc:
                desc.Directive(_directive);
                break;

            case InputObjectTypeDescriptor desc:
                desc.Directive(_directive);
                break;

            case InterfaceFieldDescriptor desc:
                desc.Directive(_directive);
                break;

            case InterfaceTypeDescriptor desc:
                desc.Directive(_directive);
                break;

            case ObjectFieldDescriptor desc:
                desc.Directive(_directive);
                break;

            case ObjectTypeDescriptor desc:
                desc.Directive(_directive);
                break;

            case SchemaTypeDescriptor desc:
                desc.Directive(_directive);
                break;

            case UnionTypeDescriptor desc:
                desc.Directive(_directive);
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(descriptor));
        }

        OnConfigure(context, _directive, element);
    }

    protected virtual void OnConfigure(
        IDescriptorContext context,
        TDirective descriptor,
        ICustomAttributeProvider element)
    {
    }
}
