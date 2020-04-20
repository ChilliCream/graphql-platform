using System;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Configuration;
using System.Collections.Generic;
using HotChocolate.Language;

#nullable enable

namespace HotChocolate.Types
{
    public abstract class FieldBase<TType, TDefinition>
        : IField
        , IHasDirectives
        , IHasClrType
        where TType : IType
        where TDefinition : FieldDefinitionBase, IHasSyntaxNode
    {
        private readonly ISyntaxNode? _syntaxNode;
        private TDefinition? _definition;

        protected FieldBase(TDefinition definition)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            _definition = definition;
            _syntaxNode = definition.SyntaxNode;

            Name = definition.Name.EnsureNotEmpty(nameof(definition.Name));
            Description = definition.Description;

            DeclaringType = default!;
            Type = default!;
            ContextData = default!;
            Directives = default!;
            ClrType = default!;
        }

        ISyntaxNode? IHasSyntaxNode.SyntaxNode => _syntaxNode;

        public ITypeSystemObject DeclaringType { get; private set; }

        public NameString Name { get; }

        public string? Description { get; }

        public TType Type { get; private set; }

        public IDirectiveCollection Directives { get; private set; }

        public virtual Type ClrType { get; private set; }

        public IReadOnlyDictionary<string, object?> ContextData { get; private set; }

        internal void CompleteField(ICompletionContext context)
        {
            OnCompleteField(context, _definition!);

            ContextData = _definition!.ContextData;
            _definition = null;
        }

        protected virtual void OnCompleteField(
            ICompletionContext context,
            TDefinition definition)
        {
            DeclaringType = context.Type;
            Type = context.GetType<TType>(definition.Type);
            ClrType = Type is IHasClrType hasClrType ? hasClrType.ClrType : typeof(object);

            var directives = new DirectiveCollection(this, definition.Directives);
            directives.CompleteCollection(context);
            Directives = directives;
        }
    }
}
