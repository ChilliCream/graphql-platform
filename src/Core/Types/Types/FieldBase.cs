using System;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Configuration;
using System.Collections.Generic;

namespace HotChocolate.Types
{
    public abstract class FieldBase<TType, TDefinition>
        : IField
        , IHasDirectives
        , IHasClrType
        where TType : IType
        where TDefinition : FieldDefinitionBase
    {
        private TDefinition _definition;
        private Dictionary<string, object> _contextData;

        protected FieldBase(TDefinition definition)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            _definition = definition;
            Name = definition.Name.EnsureNotEmpty(nameof(definition.Name));
            Description = definition.Description;
        }

        public ITypeSystemObject DeclaringType { get; private set; }

        public NameString Name { get; }

        public string Description { get; }

        public TType Type { get; private set; }

        public IDirectiveCollection Directives { get; private set; }

        public virtual Type ClrType { get; private set; }

        public IReadOnlyDictionary<string, object> ContextData =>
            _contextData;

        internal void CompleteField(ICompletionContext context)
        {
            OnCompleteField(context, _definition);

            _contextData = new Dictionary<string, object>(
                _definition.ContextData);
            _definition = null;
        }

        protected virtual void OnCompleteField(
            ICompletionContext context,
            TDefinition definition)
        {
            DeclaringType = context.Type;
            Type = context.GetType<TType>(_definition.Type);
            ClrType = Type is IHasClrType hasClrType
                ? hasClrType.ClrType
                : typeof(object);

            var directives = new DirectiveCollection(
                this, _definition.Directives);
            directives.CompleteCollection(context);
            Directives = directives;
        }
    }
}
