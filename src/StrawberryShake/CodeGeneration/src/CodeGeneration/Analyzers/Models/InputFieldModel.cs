using System;
using HotChocolate;
using HotChocolate.Types;

namespace StrawberryShake.CodeGeneration.Analyzers.Models
{
    /// <summary>
    /// Represents an input object field.
    /// </summary>
    public class InputFieldModel : IFieldModel
    {
        /// <summary>
        /// Initializes a new instance of <see cref="InputFieldModel" />
        /// </summary>
        /// <param name="name">The property name.</param>
        /// <param name="description">The property xml documentation summary.</param>
        /// <param name="field"></param>
        /// <param name="type"></param>
        public InputFieldModel(
            NameString name,
            string? description,
            IInputField field,
            IInputType type)
        {
            Name = name.EnsureNotEmpty(nameof(name));
            Description = description;
            Field = field ?? throw new ArgumentNullException(nameof(field));
            Type = type ?? throw new ArgumentNullException(nameof(type));
        }

        /// <summary>
        /// Gets the property name.
        /// </summary>
        public NameString Name { get; }

        /// <summary>
        /// Gets the property xml documentation summary.
        /// </summary>
        public string? Description { get; }

        public IInputField Field { get; }

        IField IFieldModel.Field => Field;

        public IInputType Type { get; }

        IType IFieldModel.Type => Type;
    }
}
