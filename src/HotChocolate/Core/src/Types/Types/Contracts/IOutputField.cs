#nullable enable

namespace HotChocolate.Types
{
    /// <summary>
    /// Represents an output field on a interface or object.
    /// </summary>
    public interface IOutputField : IField
    {
        /// <summary>
        /// Defines if this field is a introspection field.
        /// </summary>
        bool IsIntrospectionField { get; }

        /// <summary>
        /// Defines if this field is deprecated.
        /// </summary>
        bool IsDeprecated { get; }

        /// <summary>
        /// Gets the deprecation reason.
        /// </summary>
        string? DeprecationReason { get; }

        /// <summary>
        /// Gets the return type of this field.
        /// </summary>
        IOutputType Type { get; }

        /// <summary>
        /// Gets the field arguments.
        /// </summary>
        IFieldCollection<IInputField> Arguments { get; }

        /// <summary>
        /// Gets the type that declares this field.
        /// </summary>
        new IComplexOutputType DeclaringType { get; }
    }
}
