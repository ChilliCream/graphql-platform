using System;
using System.Collections.Generic;
using HotChocolate.Configuration;
using HotChocolate.Properties;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Descriptors
{
    internal class TypeConfiguration<T>
        : ILazyTypeConfiguration
        where T : DefinitionBase
    {
        private readonly List<TypeDependency> _dependencies =
            new List<TypeDependency>();

        public ApplyConfigurationOn On { get; set; }

        public Action<ITypeCompletionContext, T> Configure { get; set; }

        public T Definition { get; set; }

        public ICollection<TypeDependency> Dependencies => _dependencies;

        IReadOnlyList<TypeDependency> ILazyTypeConfiguration.Dependencies =>
            _dependencies;

        void ILazyTypeConfiguration.Configure(ITypeCompletionContext context)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (Definition is null)
            {
                throw new InvalidOperationException(
                    TypeResources.TypeConfiguration_DefinitionIsNull);
            }

            if (Configure is null)
            {
                throw new InvalidOperationException(
                    TypeResources.TypeConfiguration_ConfigureIsNull);
            }

            Configure(context, Definition);
        }
    }
}
