using System.Collections.Generic;

#nullable enable

namespace HotChocolate.Types.Descriptors.Definitions
{
    public class DefinitionBase
    {
        protected DefinitionBase() { }

        /// <summary>
        /// Gets or sets the name the type shall have.
        /// </summary>
        public NameString Name { get; set; }

        // <summary>
        /// Gets or sets the description the type shall have.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Get access to context data that are copied to the type
        /// and can be used for customizations.
        /// </summary>
        public IDictionary<string, object> ContextData { get; } =
            new Dictionary<string, object>();

        /// <summary>
        /// Gets access to additional type dependencies.
        /// </summary>
        public ICollection<TypeDependency> Dependencies { get; } =
            new List<TypeDependency>();

        public ICollection<ILazyTypeConfiguration> Configurations { get; } =
            new List<ILazyTypeConfiguration>();

        internal virtual IEnumerable<ILazyTypeConfiguration> GetConfigurations()
        {
            return Configurations;
        }
    }
}
