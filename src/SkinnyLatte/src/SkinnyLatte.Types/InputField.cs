using HotChocolate.Language;

namespace SkinnyLatte.Types;

public abstract class InputField : Field
{
    public InputField(
        string name,
        IInputType type,
        string? deprecationReason,
        IValueNode? defaultValue,
        IReadOnlyDictionary<string, object?> contextData,
        object directives)
        :  base(name, deprecationReason, contextData, directives)
    {
        Type = type;
        DefaultValue = defaultValue;
    }

    /// <summary>
    /// Gets the type of this input field.
    /// </summary>
    public sealed override IInputType Type { get; }

    /// <summary>
    /// Gets the default value literal of this field.
    /// </summary>
    public IValueNode? DefaultValue { get; }
}
