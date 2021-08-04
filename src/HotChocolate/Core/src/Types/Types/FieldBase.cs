using System;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Configuration;
using System.Collections.Generic;
using HotChocolate.Language;

#nullable enable

namespace HotChocolate.Types
{
    public abstract class FieldBase<TType, TDefinition> : IField
        where TType : IType
        where TDefinition : FieldDefinitionBase, IHasSyntaxNode
    {
        private TDefinition? _definition;

        protected FieldBase(TDefinition definition, int index)
        {
            _definition = definition ?? throw new ArgumentNullException(nameof(definition));
            Index = index;

            SyntaxNode = definition.SyntaxNode;
            Name = definition.Name.EnsureNotEmpty(nameof(definition.Name));
            Description = definition.Description;
            DeclaringType = default!;
            Type = default!;
            ContextData = default!;
            Directives = default!;
            RuntimeType = default!;
        }

        /// <inheritdoc />
        public NameString Name { get; }

        /// <inheritdoc />
        public string? Description { get; }

        /// <inheritdoc />
        public ISyntaxNode? SyntaxNode { get; }

        /// <inheritdoc />
        public ITypeSystemObject DeclaringType { get; private set; }

        /// <inheritdoc />
        public FieldCoordinate Coordinate { get; private set; }

        /// <inheritdoc />
        public int Index { get; }

        public TType Type { get; private set; }

        /// <inheritdoc />
        public IDirectiveCollection Directives { get; private set; }

        /// <inheritdoc />
        public virtual Type RuntimeType { get; private set; }

        /// <inheritdoc />
        public IReadOnlyDictionary<string, object?> ContextData { get; private set; }

        internal void CompleteField(
            ITypeCompletionContext context,
            ITypeSystemMember declaringMember)
        {
            OnCompleteField(context, declaringMember, _definition!);

            ContextData = _definition!.GetContextData();
            _definition = null;
        }

        protected virtual void OnCompleteField(
            ITypeCompletionContext context,
            ITypeSystemMember declaringMember,
            TDefinition definition)
        {
            DeclaringType = context.Type;
            Type = context.GetType<TType>(definition.Type!);
            RuntimeType = Type is IHasRuntimeType hrt ? hrt.RuntimeType : typeof(object);
            Coordinate = declaringMember is IField field
                ? new FieldCoordinate(context.Type.Name, field.Name, definition.Name)
                : new FieldCoordinate(context.Type.Name, definition.Name);
            Directives = DirectiveCollection.CreateAndComplete(
                context, this, definition.GetDirectives());
        }
    }
}
