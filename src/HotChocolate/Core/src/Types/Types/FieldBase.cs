using System;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Configuration;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Properties;

#nullable enable

namespace HotChocolate.Types
{
    public abstract class FieldBase<TType, TDefinition> : IField
        where TType : IType
        where TDefinition : FieldDefinitionBase, IHasSyntaxNode
    {
        private readonly ISyntaxNode? _syntaxNode;
        private TDefinition? _definition;

        protected FieldBase(TDefinition definition, FieldCoordinate fieldCoordinate)
        {
            _definition = definition ?? throw new ArgumentNullException(nameof(definition));
            _syntaxNode = definition.SyntaxNode;

            Name = definition.Name.EnsureNotEmpty(nameof(definition.Name));
            Description = definition.Description;
            Coordinate = fieldCoordinate.HasValue
                ? fieldCoordinate
                : FieldCoordinate.CreateWithoutType(definition.Name);

            DeclaringType = default!;
            Type = default!;
            ContextData = default!;
            Directives = default!;
            RuntimeType = default!;
        }

        ISyntaxNode? IHasSyntaxNode.SyntaxNode => _syntaxNode;

        public ITypeSystemObject DeclaringType { get; private set; }

        public FieldCoordinate Coordinate { get; protected set; }

        public NameString Name { get; }

        public string? Description { get; }

        public TType Type { get; private set; }

        public IDirectiveCollection Directives { get; private set; }

        public virtual Type RuntimeType { get; private set; }

        public IReadOnlyDictionary<string, object?> ContextData { get; private set; }

        internal void CompleteField(ITypeCompletionContext context)
        {
            OnCompleteField(context, _definition!);

            ContextData = _definition!.GetContextData();
            _definition = null;
        }

        protected virtual void OnCompleteField(
            ITypeCompletionContext context,
            TDefinition definition)
        {
            DeclaringType = context.Type;
            Type = context.GetType<TType>(definition.Type);
            RuntimeType = Type is IHasRuntimeType hasClrType
                ? hasClrType.RuntimeType
                : typeof(object);

            Directives =
                DirectiveCollection.CreateAndComplete(context, this, definition.GetDirectives());


            if (!DeclaringType.Name.Equals(Coordinate.TypeName))
            {
                Coordinate = Coordinate.With(typeName: DeclaringType.Name);
            }
        }
    }
}
