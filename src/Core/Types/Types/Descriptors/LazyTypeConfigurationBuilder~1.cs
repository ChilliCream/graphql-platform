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
        private Action<ICompletionContext, T> _configure;
        private T _definition;
        private List<(ITypeReference reference, bool complete)> _dependencies =
            new List<(ITypeReference, bool)>();

        public LazyTypeConfigurationBuilder<T> On(ApplyConfigurationOn kind)
        {
            _on = kind;

            if (kind == ApplyConfigurationOn.Naming)
            {
                _completeKind = TypeDependencyKind.Named;
            }
            else
            {
                _completeKind = TypeDependencyKind.Completed;
            }

            return this;
        }

        public LazyTypeConfigurationBuilder<T> Configure(
            Action<ICompletionContext, T> configure)
        {
            if (configure is null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            _configure = configure;
            return this;
        }

        public LazyTypeConfigurationBuilder<T> Definition(T definition)
        {
            if (definition is null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            _definition = definition;
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
            if (_configure == null)
            {
                // TODO : Resources
                throw new InvalidOperationException(
                    "You have to set a configuration function " +
                    "before you can build.");
            }

            if (_definition == null)
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

            foreach (var dependency in _dependencies)
            {
                configuration.Dependencies.Add(new TypeDependency
                (
                    dependency.reference,
                    dependency.complete
                        ? _completeKind
                        : TypeDependencyKind.Default
                ));
            }

            return configuration;
        }
    }
}
