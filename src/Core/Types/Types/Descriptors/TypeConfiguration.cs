using System;
using System.Collections.Generic;
using HotChocolate.Configuration;
using HotChocolate.Properties;
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

        public Action<ICompletionContext, T> Configure { get; set; }

        public T Definition { get; set; }

        public ICollection<TypeDependency> Dependencies => _dependencies;

        IReadOnlyList<TypeDependency> ITypeConfigration.Dependencies =>
            _dependencies;

        void ITypeConfigration.Configure(ICompletionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (Definition == null)
            {
                throw new InvalidOperationException(
                    TypeResources.TypeConfiguration_DefinitionIsNull);
            }

            if (Configure == null)
            {
                throw new InvalidOperationException(
                    TypeResources.TypeConfiguration_ConfigureIsNull);
            }

            Configure(context, Definition);
        }
    }
}
