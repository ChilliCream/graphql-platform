using System.Linq;
using System;
using System.Collections.Generic;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Configuration;

namespace HotChocolate.Types
{
    public abstract class TypeSystemObjectBase<TDefinition>
        : TypeSystemObjectBase
        where TDefinition : DefinitionBase
    {
        private TDefinition _definition;
        private Dictionary<string, object> _contextData;
        private IReadOnlyCollection<ITypeConfigration> _configrations;

        protected TypeSystemObjectBase() { }

        public override IReadOnlyDictionary<string, object> ContextData =>
            _contextData;

        internal TDefinition Definition => _definition;

        internal sealed override void Initialize(IInitializationContext context)
        {
            _definition = CreateDefinition(context);
            _configrations = _definition?.GetConfigurations().ToList();

            if (_definition == null)
            {
                // TODO : exception type
                // TODO : resources
                throw new InvalidOperationException();
            }

            RegisterConfigurationDependencies(context);
            OnRegisterDependencies(context, _definition);
            base.Initialize(context);
        }

        protected abstract TDefinition CreateDefinition(
            IInitializationContext context);

        protected virtual void OnRegisterDependencies(
            IInitializationContext context,
            TDefinition definition)
        {
        }

        internal sealed override void CompleteName(ICompletionContext context)
        {
            ExecuteConfigurations(context, ConfigurationKind.Naming);
            OnCompleteName(context, _definition);
            base.CompleteName(context);
        }

        protected virtual void OnCompleteName(
            ICompletionContext context,
            TDefinition definition)
        {
            if (definition.Name.IsEmpty)
            {
                // TODO : exception type
                // TODO : resources
                throw new InvalidOperationException(
                    "The type is initialize bla bla ...");
            }

            Name = definition.Name;
        }

        internal sealed override void CompleteType(ICompletionContext context)
        {
            ExecuteConfigurations(context, ConfigurationKind.Completion);

            Description = _definition.Description;

            OnCompleteType(context, _definition);

            _contextData = new Dictionary<string, object>(
                _definition.ContextData);
            _definition = null;
            _configrations = null;

            base.CompleteType(context);
        }

        protected virtual void OnCompleteType(
            ICompletionContext context,
            TDefinition definition)
        {
        }

        private void RegisterConfigurationDependencies(
            IInitializationContext context)
        {
            foreach (var group in
                _configrations.SelectMany(t => t.Dependencies)
                    .GroupBy(t => t.Kind))
            {
                context.RegisterDependencyRange(
                    group.Select(t => t.TypeReference),
                    group.Key);
            }
        }

        private void ExecuteConfigurations(
            ICompletionContext context,
            ConfigurationKind kind)
        {
            foreach (ITypeConfigration configuration in
                _configrations.Where(t => t.Kind == kind))
            {
                configuration.Configure(context);
            }
        }
    }
}
