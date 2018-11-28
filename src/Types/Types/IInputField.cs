using HotChocolate.Language;

namespace HotChocolate.Types
{
    /// <summary>
    /// Represents an input field. Input fields can be arguments of fields
    /// or fields of an input objects.
    /// </summary>
    public interface IInputField
        : IField
    {
        /// <summary>
        /// Gets the type of this input field.
        /// </summary>
        IInputType Type { get; }

        /// <summary>
        /// Gets the default value literal of this field.
        /// </summary>
        IValueNode DefaultValue { get; }

        IDirectiveCollection Directives { get; }
    }
}
