using System;
using System.Collections.Generic;
using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types
{
    public class TypeConfiguration<T>
        : ITypeConfigration
        where T : DefinitionBase
    {
        public TypeConfiguration(
            Action<T, IReadOnlyList<ITypeSystemObject>> configure)
            : this(
                ConfigurationKind.Creation,
                configure,
                Array.Empty<TypeDependency>())
        {
        }

        public TypeConfiguration(
            ConfigurationKind kind,
            Action<T, IReadOnlyList<ITypeSystemObject>> configure)
            : this(kind, configure, Array.Empty<TypeDependency>())
        {
        }

        public TypeConfiguration(
            ConfigurationKind kind,
            Action<T, IReadOnlyList<ITypeSystemObject>> configure,
            TypeDependency dependency)
            : this(kind, configure, new[] { dependency })
        {
        }

        public TypeConfiguration(
            ConfigurationKind kind,
            Action<T, IReadOnlyList<ITypeSystemObject>> configure,
            IReadOnlyList<TypeDependency> dependencies)
        {
            Kind = kind;
            Configure = configure
                ?? throw new ArgumentNullException(nameof(configure));
            Dependencies = dependencies
                ?? throw new ArgumentNullException(nameof(dependencies));

            if (kind == ConfigurationKind.Creation && dependencies.Count > 0)
            {
                // TODO : resources
                throw new ArgumentException(
                    "Dependencies are only allowed after creation");
            }
        }

        public ConfigurationKind Kind { get; }

        public Action<T, IReadOnlyList<ITypeSystemObject>> Configure { get; }

        public IReadOnlyList<TypeDependency> Dependencies { get; }

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
                Configure(t);
            }
            else
            {
                // TODO : Resources
                throw new ArgumentException("Invalid definition.");
            }
        }
    }

    public enum ConfigurationKind
    {
        Creation,
        Naming,
        Completion
    }
}
