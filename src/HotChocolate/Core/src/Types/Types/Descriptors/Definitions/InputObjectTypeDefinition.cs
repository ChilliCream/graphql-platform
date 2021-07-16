using System;
using System.Collections.Generic;
using HotChocolate.Language;

#nullable enable

namespace HotChocolate.Types.Descriptors.Definitions
{
    /// <summary>
    /// Defines the properties of a GraphQL input object type.
    /// </summary>
    public class InputObjectTypeDefinition : TypeDefinitionBase<InputObjectTypeDefinitionNode>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="EnumTypeDefinition"/>.
        /// </summary>
        public InputObjectTypeDefinition() { }

        /// <summary>
        /// Initializes a new instance of <see cref="EnumTypeDefinition"/>.
        /// </summary>
        public InputObjectTypeDefinition(
            NameString name,
            string? description = null,
            Type? runtimeType = null)
            : base(runtimeType ?? typeof(object))
        {
            Name = name;
            Description = description;
        }

        /// <summary>
        /// Gets the fields.
        /// </summary>
        public IBindableList<InputFieldDefinition> Fields { get; } =
            new BindableList<InputFieldDefinition>();

        internal override IEnumerable<ILazyTypeConfiguration> GetConfigurations()
        {
            var configs = new List<ILazyTypeConfiguration>();

            configs.AddRange(Configurations);

            foreach (InputFieldDefinition field in Fields)
            {
                configs.AddRange(field.Configurations);
            }

            return configs;
        }
    }
}
