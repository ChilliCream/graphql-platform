using System.Reflection;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types.Composite;

/// <summary>
/// <para>
/// Applies the @context directive to the type to declare that its instances provide
/// the named context. A type can declare multiple contexts.
/// </para>
/// <para>
/// <code language="graphql">
/// type User @context(name: "userContext") {
///   id: ID!
///   userCurrency: Currency!
/// }
/// </code>
/// </para>
/// </summary>
[AttributeUsage(
    AttributeTargets.Class
    | AttributeTargets.Interface
    | AttributeTargets.Struct,
    AllowMultiple = true)]
public sealed class ContextAttribute : DescriptorAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContextAttribute"/> class.
    /// </summary>
    /// <param name="name">The name of the context.</param>
    /// <exception cref="ArgumentException">
    /// The <paramref name="name"/> is <c>null</c> or empty.
    /// </exception>
    public ContextAttribute(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        Name = name;
    }

    /// <summary>
    /// Gets the name of the context.
    /// </summary>
    public string Name { get; }

    protected internal override void TryConfigure(
        IDescriptorContext context,
        IDescriptor descriptor,
        ICustomAttributeProvider? attributeProvider)
    {
        switch (descriptor)
        {
            case IObjectTypeDescriptor objectTypeDescriptor:
                objectTypeDescriptor.Context(Name);
                break;

            case IInterfaceTypeDescriptor interfaceTypeDescriptor:
                interfaceTypeDescriptor.Context(Name);
                break;

            case IUnionTypeDescriptor unionTypeDescriptor:
                unionTypeDescriptor.Context(Name);
                break;

            default:
                throw new NotSupportedException(
                    $"The {descriptor.GetType().Name} descriptor is not supported.");
        }
    }
}
