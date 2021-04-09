using System;
using System.Collections.Generic;
using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Descriptors
{
    public class LazyTypeConfigurationBuilder<T>
        where T : DefinitionBase
    {
        private ApplyConfigurationOn _on = ApplyConfigurationOn.Naming;
        private TypeDependencyKind _completeKind = TypeDependencyKind.Named;
        private Action<ITypeCompletionContext, T> _configure;
        private T _definition;
        private readonly List<(ITypeReference r, bool c)> _dependencies = new();

        public LazyTypeConfigurationBuilder<T> On(ApplyConfigurationOn kind)
        {
            _on = kind;

            _completeKind = kind == ApplyConfigurationOn.Naming
                 ? TypeDependencyKind.Named
                 : TypeDependencyKind.Completed;

            return this;
        }

        public LazyTypeConfigurationBuilder<T> Configure(
            Action<ITypeCompletionContext, T> configure)
        {
            _configure = configure ?? throw new ArgumentNullException(nameof(configure));
            return this;
        }

        public LazyTypeConfigurationBuilder<T> Definition(T definition)
        {
            _definition = definition ?? throw new ArgumentNullException(nameof(definition));
            return this;
        }

        public LazyTypeConfigurationBuilder<T> DependsOn(
            ITypeReference typeReference, bool mustBeCompleted)
        {
            if (typeReference is null)
            {
                throw new ArgumentNullException(nameof(typeReference));
            }

            _dependencies.Add((typeReference, mustBeCompleted));
            return this;
        }

        public ILazyTypeConfiguration Build()
        {
            if (_configure is null)
            {
                // TODO : Resources
                throw new InvalidOperationException(
                    "You have to set a configuration function " +
                    "before you can build.");
            }

            if (_definition is null)
            {
                // TODO : Resources
                throw new InvalidOperationException(
                    "You have to set the definition " +
                    "before you can build.");
            }

            var configuration = new TypeConfiguration<T>
            {
                On = _on,
                Configure = _configure,
                Definition = _definition
            };

            foreach ((ITypeReference r, bool c) dependency in _dependencies)
            {
                configuration.Dependencies.Add(new TypeDependency
                (
                    dependency.r,
                    dependency.c ? _completeKind : TypeDependencyKind.Default
                ));
            }

            return configuration;
        }
    }
}
