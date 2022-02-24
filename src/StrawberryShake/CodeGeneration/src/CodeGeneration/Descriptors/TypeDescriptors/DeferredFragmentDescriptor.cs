using HotChocolate;

namespace StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;

/// <summary>
/// This descriptor refers to a deferred fragment.
/// </summary>
public sealed class DeferredFragmentDescriptor
{
    /// <summary>
    /// Initializes a new instance of <see cref="DeferredFragmentDescriptor"/>.
    /// </summary>
    /// <param name="label">The defer label.</param>
    /// <param name="interfaceName">The interface name of the deferred fragment.</param>
    /// <param name="className">The class name of the deferred fragment.</param>
    public DeferredFragmentDescriptor(
        string label,
        NameString interfaceName,
        NameString className)
    {
        Label = label;
        InterfaceName = interfaceName;
        ClassName = className;
        FragmentIndicator = $"Is{ClassName.Value}Fulfilled";
        FragmentIndicatorField = $"_is{ClassName.Value}Fulfilled";
    }

    /// <summary>
    /// Gets the defer label.
    /// </summary>
    public string Label { get; }

    /// <summary>
    /// Gets the interface name of the deferred fragment.
    /// </summary>
    public NameString InterfaceName { get; }

    /// <summary>
    /// The interface descriptor representing this deferred fragment.
    /// </summary>
    public InterfaceTypeDescriptor Interface { get; private set; } = default!;

    /// <summary>
    /// Gets the class name of the deferred fragment.
    /// </summary>
    public NameString ClassName { get; }

    /// <summary>
    /// The class descriptor representing this deferred fragment.
    /// </summary>
    public ObjectTypeDescriptor Class { get; private set; } = default!;

    /// <summary>
    /// The entity property that represents the fragment indicator.
    /// </summary>
    public NameString FragmentIndicator { get; private set; }

    /// <summary>
    /// The result field that represents the fragment indicator.
    /// </summary>
    public NameString FragmentIndicatorField { get; private set; }

    internal void Complete(
        InterfaceTypeDescriptor @interface,
        ObjectTypeDescriptor @class)
    {
        Interface = @interface;
        Class = @class;
    }
}
