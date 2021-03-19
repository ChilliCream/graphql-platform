using System;
using System.Collections.Generic;
using System.Collections.Immutable;

#nullable enable

namespace HotChocolate.Types.Descriptors.Definitions
{
    /// <summary>
    /// A type system definition is used in the type initialization to store properties
    /// of a type system object.
    /// </summary>
    public class DefinitionBase
    {
        private List<TypeDependency>? _dependencies;
        private List<ILazyTypeConfiguration>? _configurations;
        private ExtensionData? _contextData;

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
        public ExtensionData ContextData => _contextData ??= new ExtensionData();

        /// <summary>
        /// Gets access to additional type dependencies.
        /// </summary>
        public IList<TypeDependency> Dependencies =>
            _dependencies ??= new List<TypeDependency>();

        /// <summary>
        /// Gets configurations that shall be applied at a later point.
        /// </summary>
        public IList<ILazyTypeConfiguration> Configurations =>
            _configurations ??= new List<ILazyTypeConfiguration>();

        /// <summary>
        /// Gets lazy configuration of this definition and all dependent definitions.
        /// </summary>
        internal virtual IEnumerable<ILazyTypeConfiguration> GetConfigurations()
        {
            if (_configurations is null)
            {
                return Array.Empty<ILazyTypeConfiguration>();
            }

            return _configurations;
        }

        /// <summary>
        /// Gets access to additional type dependencies.
        /// </summary>
        internal IReadOnlyList<TypeDependency> GetDependencies()
        {
            if (_dependencies is null)
            {
                return Array.Empty<TypeDependency>();
            }

            return _dependencies;
        }

        /// <summary>
        /// Get access to context data that are copied to the type
        /// and can be used for customizations.
        /// </summary>
        internal IReadOnlyDictionary<string, object?> GetContextData()
        {
            if (_contextData is null)
            {
                return ImmutableDictionary<string, object?>.Empty;
            }

            return _contextData;
        }

        protected void CopyTo(DefinitionBase target)
        {
            target._dependencies = _dependencies;
            target._configurations = _configurations;
            target._contextData = _contextData;
            target.Name = Name;
            target.Description = Description;
        }
    }
}
