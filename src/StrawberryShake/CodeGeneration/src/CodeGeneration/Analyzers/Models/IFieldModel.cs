using HotChocolate;
using HotChocolate.Types;

namespace StrawberryShake.CodeGeneration.Analyzers.Models
{
    /// <summary>
    /// Represents a field model.
    /// </summary>
    public interface IFieldModel
    {
        /// <summary>
        /// Gets the member name.
        /// </summary>
        NameString Name { get; }

        /// <summary>
        /// Gets the member xml documentation summary.
        /// </summary>
        string? Description { get; }

        /// <summary>
        /// Gets the field.
        /// </summary>
        IField Field { get; }

        /// <summary>
        /// Gets the field schema type.
        /// </summary>
        IType Type { get; }
    }
}
