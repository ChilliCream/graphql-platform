using System;
using System.Collections.Generic;
using HotChocolate.Language;

#nullable enable

namespace HotChocolate.Types.Descriptors.Definitions
{
    /// <summary>
    /// Defines the properties of a GraphQL enum type.
    /// </summary>
    public class EnumTypeDefinition : TypeDefinitionBase<EnumTypeDefinitionNode>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="EnumTypeDefinition"/>.
        /// </summary>
        public EnumTypeDefinition() { }

        /// <summary>
        /// Initializes a new instance of <see cref="EnumTypeDefinition"/>.
        /// </summary>
        public EnumTypeDefinition(
            NameString name,
            string? description = null,
            Type? runtimeType = null)
            : base(runtimeType ?? typeof(object))
        {
            Name = name;
            Description = description;
        }

        /// <summary>
        /// Gets the enum values.
        /// </summary>
        public IBindableList<EnumValueDefinition> Values { get; } =
            new BindableList<EnumValueDefinition>();

        internal override IEnumerable<ILazyTypeConfiguration> GetConfigurations()
        {
            var configs = new List<ILazyTypeConfiguration>();

            configs.AddRange(Configurations);

            foreach (EnumValueDefinition value in Values)
            {
                configs.AddRange(value.Configurations);
            }

            return configs;
        }
    }
}
