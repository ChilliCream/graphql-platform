using System.Collections.Generic;

#nullable enable

namespace HotChocolate.Types.Descriptors.Definitions
{
    /// <summary>
    /// A type system definition is used in the type initialization to store properties
    /// of a type system object.
    /// </summary>
    public class DefinitionBase
    {
        protected DefinitionBase() { }

        /// <summary>
        /// Gets or sets the name the type shall have.
        /// </summary>
        public NameString Name { get; set; }

        /// <summary>
        /// Gets or sets the description the type shall have.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Get access to context data that are copied to the type
        /// and can be used for customizations.
        /// </summary>
        public ExtensionData ContextData { get; } =
            new ExtensionData();

        /// <summary>
        /// Gets access to additional type dependencies.
        /// </summary>
        public IList<TypeDependency> Dependencies { get; } =
            new List<TypeDependency>();

        /// <summary>
        /// Gets configurations that shall be applied at a later point.
        /// </summary>
        public IList<ILazyTypeConfiguration> Configurations { get; } =
            new List<ILazyTypeConfiguration>();

        /// <summary>
        /// Gets lazy configuration of this definition and all dependent definitions.
        /// </summary>
        internal virtual IEnumerable<ILazyTypeConfiguration> GetConfigurations()
        {
            return Configurations;
        }
    }
}
