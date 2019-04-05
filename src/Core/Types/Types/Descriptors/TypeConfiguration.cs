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

        public Action<ICompletionContext, T> Configure
        { get; set; }

        public ICollection<TypeDependency> Dependencies => _dependencies;

        IReadOnlyList<TypeDependency> ITypeConfigration.Dependencies =>
            _dependencies;

        void ITypeConfigration.Configure(
            ICompletionContext context,
            DefinitionBase definition)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }


            if (definition is T t)
            {
                Configure(context, t);
            }
            else
            {
                // TODO : Resources
                throw new ArgumentException("Invalid definition.");
            }
        }
    }


}
