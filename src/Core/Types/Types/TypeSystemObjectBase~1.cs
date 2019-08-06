using System.Linq;
using System;
using System.Collections.Generic;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Configuration;
using HotChocolate.Properties;
using System.Globalization;

namespace HotChocolate.Types
{
    public abstract class TypeSystemObjectBase<TDefinition>
        : TypeSystemObjectBase
        where TDefinition : DefinitionBase
    {
        private TDefinition _definition;
        private Dictionary<string, object> _contextData;
        private IReadOnlyCollection<ILazyTypeConfiguration> _configrations;

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
                throw new InvalidOperationException(
                    TypeResources.TypeSystemObjectBase_DefinitionIsNull);
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
            ExecuteConfigurations(context, ApplyConfigurationOn.Naming);
            OnCompleteName(context, _definition);

            if (Name.IsEmpty)
            {
                context.ReportError(SchemaErrorBuilder.New()
                    .SetMessage(string.Format(
                        CultureInfo.InvariantCulture,
                        TypeResources.TypeSystemObjectBase_NameIsNull,
                        GetType().FullName))
                    .SetCode(TypeErrorCodes.NoName)
                    .SetTypeSystemObject(this)
                    .Build());
            }

            base.CompleteName(context);
        }

        protected virtual void OnCompleteName(
            ICompletionContext context,
            TDefinition definition)
        {
            if (definition.Name.HasValue)
            {
                Name = definition.Name;
            }
        }

        internal sealed override void CompleteType(ICompletionContext context)
        {
            ExecuteConfigurations(context, ApplyConfigurationOn.Completion);

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
            ApplyConfigurationOn kind)
        {
            foreach (ILazyTypeConfiguration configuration in
                _configrations.Where(t => t.On == kind))
            {
                configuration.Configure(context);
            }
        }
    }
}
