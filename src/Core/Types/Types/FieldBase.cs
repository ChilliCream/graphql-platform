using System;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Configuration;

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

        protected FieldBase(TDefinition definition)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            if (string.IsNullOrEmpty(definition.Name))
            {
                throw new ArgumentException(
                    "The name of a field mustn't be null or empty.",
                    nameof(definition));
            }

            _definition = definition;
            Name = definition.Name;
            Description = definition.Description;
        }

        public ITypeSystemObject DeclaringType { get; private set; }

        public NameString Name { get; }

        public string Description { get; }

        public TType Type { get; private set; }

        public IDirectiveCollection Directives { get; private set; }

        public virtual Type ClrType { get; private set; }

        internal void CompleteField(ICompletionContext context)
        {
            DeclaringType = context.Type;
            Type = context.GetType<TType>(_definition.Type);
            ClrType = Type.NamedType() is IHasClrType hasClrType
                ? hasClrType.ClrType
                : typeof(object);

            var directives = new DirectiveCollection(
                this, _definition.Directives);
            directives.CompleteCollection(context);
            Directives = directives;

            OnCompleteField(context, _definition);
            _definition = null;
        }

        protected virtual void OnCompleteField(
            ICompletionContext context,
            TDefinition definition)
        {
        }
    }
}
