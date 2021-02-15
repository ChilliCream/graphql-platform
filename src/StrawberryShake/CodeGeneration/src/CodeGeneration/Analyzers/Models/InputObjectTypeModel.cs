using System;
using System.Collections.Generic;
using HotChocolate;
using HotChocolate.Types;

namespace StrawberryShake.CodeGeneration.Analyzers.Models
{
    /// <summary>
    /// Represents an input object type model.
    /// </summary>
    public sealed class InputObjectTypeModel : ITypeModel
    {
        /// <summary>
        /// Initializes a new instance of <see cref="InputObjectTypeModel" />
        /// </summary>
        /// <param name="name">The class name.</param>
        /// <param name="description">The class description.</param>
        /// <param name="type">The input object type.</param>
        /// <param name="fields">The field models of this input type.</param>
        public InputObjectTypeModel(
            NameString name,
            string? description,
            InputObjectType type,
            IReadOnlyList<InputFieldModel> fields)
        {
            Name = name.EnsureNotEmpty(nameof(name));
            Description = description;
            Type = type ?? throw new ArgumentNullException(nameof(type));
            Fields = fields ?? throw new ArgumentNullException(nameof(fields));
        }

        /// <summary>
        /// Gets the class name.
        /// </summary>
        public NameString Name { get; }

        /// <summary>
        /// Gets the class xml documentation summary.
        /// </summary>
        public string? Description { get; }

        /// <summary>
        /// Gets the input object type.
        /// </summary>
        public InputObjectType Type { get; }

        INamedType ITypeModel.Type => Type;

        /// <summary>
        /// Gets the field models of this input type.
        /// </summary>
        public IReadOnlyList<InputFieldModel> Fields { get; }
    }
}
