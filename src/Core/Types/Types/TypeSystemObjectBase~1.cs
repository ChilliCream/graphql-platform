using System.Linq;
using System;
using System.Collections.Generic;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Configuration;
using HotChocolate.Properties;
using System.Globalization;

#nullable enable

namespace HotChocolate.Types
{
    public abstract class TypeSystemObjectBase<TDefinition>
        : TypeSystemObjectBase
        where TDefinition : DefinitionBase
    {
        private TDefinition? _definition;
        private Dictionary<string, object?>? _contextData;

        protected TypeSystemObjectBase() { }

        public override IReadOnlyDictionary<string, object?> ContextData
        {
            get
            {
                if (_contextData is null)
                {
                    throw new TypeInitializationException();
                }
                return _contextData;
            }
        }

        internal TDefinition? Definition
        {
            get
            {
                return _definition;
            }
        }

        internal sealed override void Initialize(IInitializationContext context)
        {
            _definition = CreateDefinition(context);

            if (_definition is null)
            {
                throw new InvalidOperationException(
                    TypeResources.TypeSystemObjectBase_DefinitionIsNull);
            }

            context.Interceptor.OnBeforeRegisterDependencies(
                context, _definition, _definition.ContextData);

            RegisterConfigurationDependencies(context, _definition);
            OnRegisterDependencies(context, _definition);

            context.Interceptor.OnAfterRegisterDependencies(
                context, _definition, _definition.ContextData);

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
            if (_definition is null)
            {
                throw new InvalidOperationException(
                    TypeResources.TypeSystemObjectBase_DefinitionIsNull);
            }

            context.Interceptor.OnBeforeCompleteName(
                context, _definition, _definition.ContextData);

            ExecuteConfigurations(context, _definition, ApplyConfigurationOn.Naming);
            OnCompleteName(context, _definition);

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
                context, _definition, _definition.ContextData);
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
            if (_definition is null)
            {
                throw new InvalidOperationException(
                    TypeResources.TypeSystemObjectBase_DefinitionIsNull);
            }

            DefinitionBase definition = _definition;

            context.Interceptor.OnBeforeCompleteType(
                context, definition, _definition.ContextData);

            ExecuteConfigurations(context, definition, ApplyConfigurationOn.Completion);

            Description = _definition.Description;

            OnCompleteType(context, _definition);

            _contextData = new Dictionary<string, object?>(_definition.ContextData);
            _definition = null;

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
            IInitializationContext context,
            DefinitionBase definition)
        {
            foreach (IGrouping<TypeDependencyKind, TypeDependency> group in
                definition.GetConfigurations()
                    .SelectMany(t => t.Dependencies)
                    .GroupBy(t => t.Kind))
            {
                context.RegisterDependencyRange(
                    group.Select(t => t.TypeReference),
                    group.Key);
            }
        }

        private void ExecuteConfigurations(
            ICompletionContext context,
            DefinitionBase definition,
            ApplyConfigurationOn kind)
        {
            foreach (ILazyTypeConfiguration configuration in
                definition.GetConfigurations().Where(t => t.On == kind))
            {
                configuration.Configure(context);
            }
        }
    }
}
