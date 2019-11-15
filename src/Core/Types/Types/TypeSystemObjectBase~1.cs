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
        private Dictionary<string, object> _contextData;
        private IReadOnlyCollection<ILazyTypeConfiguration> _configrations;

        protected TypeSystemObjectBase() { }

        public override IReadOnlyDictionary<string, object> ContextData =>
            _contextData;

        internal TDefinition Definition { get; private set; }

        internal sealed override void Initialize(IInitializationContext context)
        {
            Definition = CreateDefinition(context);
            _configrations = Definition?.GetConfigurations().ToList();

            if (Definition == null)
            {
                throw new InvalidOperationException(
                    TypeResources.TypeSystemObjectBase_DefinitionIsNull);
            }

            context.Interceptor.OnBeforeRegisterDependencies(
                context, Definition, Definition.ContextData);

            RegisterConfigurationDependencies(context);
            OnRegisterDependencies(context, Definition);

            context.Interceptor.OnAfterRegisterDependencies(
                context, Definition, Definition.ContextData);

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
            context.Interceptor.OnBeforeCompleteName(
                context, Definition, Definition.ContextData);

            ExecuteConfigurations(context, ApplyConfigurationOn.Naming);
            OnCompleteName(context, Definition);

            if (Name.IsEmpty)
            {
                context.ReportError(SchemaErrorBuilder.New()
                    .SetMessage(string.Format(
                        CultureInfo.InvariantCulture,
                        TypeResources.TypeSystemObjectBase_NameIsNull,
                        GetType().FullName))
                    .SetCode(ErrorCodes.Schema.NoName)
                    .SetTypeSystemObject(this)
                    .Build());
            }

            base.CompleteName(context);

            context.Interceptor.OnAfterCompleteName(
                context, Definition, Definition.ContextData);
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
            DefinitionBase definition = Definition;

            context.Interceptor.OnBeforeCompleteType(
                context, definition, definition.ContextData);

            ExecuteConfigurations(context, ApplyConfigurationOn.Completion);

            Description = Definition.Description;

            OnCompleteType(context, Definition);

            _contextData = new Dictionary<string, object>(
                Definition.ContextData);
            
            Definition = null;
            _configrations = null;

            base.CompleteType(context);

            context.Interceptor.OnAfterCompleteType(
                context, definition, _contextData);
        }

        protected virtual void OnCompleteType(
            ICompletionContext context,
            TDefinition definition)
        {
        }

        private void RegisterConfigurationDependencies(
            IInitializationContext context)
        {
            foreach (IGrouping<TypeDependencyKind, TypeDependency> group in
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
