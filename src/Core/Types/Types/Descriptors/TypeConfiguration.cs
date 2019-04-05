using System;
using System.Collections.Generic;
using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Descriptors
{
    internal class TypeConfiguration<T>
        : ITypeConfigration
        where T : DefinitionBase
    {
        private readonly List<TypeDependency> _dependencies =
            new List<TypeDependency>();
        public ConfigurationKind Kind { get; set; }

        public Action<T, IReadOnlyList<ITypeSystemObject>> Configure
        { get; set; }

        public ICollection<TypeDependency> Dependencies => _dependencies;

        IReadOnlyList<TypeDependency> ITypeConfigration.Dependencies =>
            _dependencies;

        void ITypeConfigration.Configure(
            DefinitionBase definition,
            IReadOnlyList<ITypeSystemObject> depenencies)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            if (depenencies == null)
            {
                throw new ArgumentNullException(nameof(depenencies));
            }

            if (definition is T t)
            {
                Configure(t, depenencies);
            }
            else
            {
                // TODO : Resources
                throw new ArgumentException("Invalid definition.");
            }
        }
    }


}
