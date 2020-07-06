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
        private ExtensionData? _contextData;

        protected TypeSystemObjectBase() { }

        public override IReadOnlyDictionary<string, object?> ContextData
        {
            get
            {
                return _contextData ?? throw new TypeInitializationException();
            }
        }

        internal TDefinition? Definition
        {
            get
            {
                return _definition;
            }
        }

        internal sealed override void Initialize(ITypeDiscoveryContext context)
        {
            _definition = CreateDefinition(context);

            if (_definition is null)
            {
                throw new InvalidOperationException(
                    TypeResources.TypeSystemObjectBase_DefinitionIsNull);
            }

            OnBeforeRegisterDependencies(context, _definition, _definition.ContextData);

            RegisterConfigurationDependencies(context, _definition);
            OnRegisterDependencies(context, _definition);

            OnAfterRegisterDependencies(context, _definition, _definition.ContextData);

            base.Initialize(context);
        }

        protected abstract TDefinition CreateDefinition(
            ITypeDiscoveryContext context);

        protected virtual void OnRegisterDependencies(
            ITypeDiscoveryContext context,
            TDefinition definition)
        {
        }

        internal sealed override void CompleteName(ITypeCompletionContext context)
        {
            if (_definition is null)
            {
                throw new InvalidOperationException(
                    TypeResources.TypeSystemObjectBase_DefinitionIsNull);
            }

            OnBeforeCompleteName(context, _definition, _definition.ContextData);

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

            OnAfterCompleteName(context, _definition, _definition.ContextData);
        }

        protected virtual void OnCompleteName(
            ITypeCompletionContext context,
            TDefinition definition)
        {
            if (definition.Name.HasValue)
            {
                Name = definition.Name;
            }
        }

        internal sealed override void CompleteType(ITypeCompletionContext context)
        {
            if (_definition is null)
            {
                throw new InvalidOperationException(
                    TypeResources.TypeSystemObjectBase_DefinitionIsNull);
            }

            TDefinition definition = _definition;

            OnBeforeCompleteType(context, definition, definition.ContextData);

            ExecuteConfigurations(context, definition, ApplyConfigurationOn.Completion);
            Description = definition.Description;
            OnCompleteType(context, definition);

            _contextData = definition.ContextData;
            _definition = null;

            base.CompleteType(context);

            OnAfterCompleteType(context, definition, _contextData);
        }

        protected virtual void OnCompleteType(
            ITypeCompletionContext context,
            TDefinition definition)
        {
        }

        private static void RegisterConfigurationDependencies(
            ITypeDiscoveryContext context,
            TDefinition definition)
        {
            foreach (var group in definition.GetConfigurations()
                .SelectMany(t => t.Dependencies)
                .GroupBy(t => t.Kind))
            {
                context.RegisterDependencyRange(
                    group.Select(t => t.TypeReference),
                    group.Key);
            }
        }

        private static void ExecuteConfigurations(
            ITypeCompletionContext context,
            TDefinition definition,
            ApplyConfigurationOn kind)
        {
            foreach (ILazyTypeConfiguration configuration in
                definition.GetConfigurations().Where(t => t.On == kind))
            {
                configuration.Configure(context);
            }
        }

        protected virtual void OnBeforeRegisterDependencies(
            ITypeDiscoveryContext context,
            DefinitionBase definition,
            IDictionary<string, object?> contextData)
        {
            context.Interceptor.OnBeforeRegisterDependencies(
                context, definition, contextData);
        }

        protected virtual void OnAfterRegisterDependencies(
            ITypeDiscoveryContext context,
            DefinitionBase definition,
            IDictionary<string, object?> contextData)
        {
            context.Interceptor.OnAfterRegisterDependencies(
                context, definition, contextData);
        }

        protected virtual void OnBeforeCompleteName(
            ITypeCompletionContext context,
            DefinitionBase definition,
            IDictionary<string, object?> contextData)
        {
            context.Interceptor.OnBeforeCompleteName(
                context, definition, contextData);
        }

        protected virtual void OnAfterCompleteName(
            ITypeCompletionContext context,
            DefinitionBase definition,
            IDictionary<string, object?> contextData)
        {
            context.Interceptor.OnAfterCompleteName(
                context, definition, contextData);
        }

        protected virtual void OnBeforeCompleteType(
            ITypeCompletionContext context,
            DefinitionBase definition,
            IDictionary<string, object?> contextData)
        {
            context.Interceptor.OnBeforeCompleteType(
                context, definition, contextData);
        }

        protected virtual void OnAfterCompleteType(
            ITypeCompletionContext context,
            DefinitionBase definition,
            IDictionary<string, object?> contextData)
        {
            context.Interceptor.OnAfterCompleteType(
                context, definition, contextData);
        }
    }
}
