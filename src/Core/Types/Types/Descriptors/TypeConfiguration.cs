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
                // TODO : resources
                throw new InvalidOperationException(
                    "The Definition mustn't be null");
            }

            if (Configure == null)
            {
                // TODO : resources
                throw new InvalidOperationException(
                    "The Configure mustn't be null");
            }

            Configure(context, Definition);
        }
    }
}
