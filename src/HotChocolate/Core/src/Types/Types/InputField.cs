using System;
using System.Collections.Generic;
using System.Reflection;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;

#nullable enable

namespace HotChocolate.Types
{
    public class InputField
        : FieldBase<IInputType, InputFieldDefinition>
        , IInputField
    {
        private Type _runtimeType = default!;

        public InputField(InputFieldDefinition definition, int index)
            : base(definition, index)
        {
            DefaultValue = definition.DefaultValue;
            Property = definition.Property;

            IReadOnlyList<IInputValueFormatter> formatters = definition.GetFormatters();
            Formatter = formatters.Count switch
            {
                0 => null,
                1 => formatters[0],
                _ => new AggregateInputValueFormatter(formatters)
            };
        }

        /// <summary>
        /// The associated syntax node from the GraphQL SDL.
        /// </summary>
        public new InputValueDefinitionNode? SyntaxNode =>
            (InputValueDefinitionNode?)base.SyntaxNode;

        /// <inheritdoc />
        public IValueNode? DefaultValue { get; private set; }

        /// <inheritdoc />
        public IInputValueFormatter? Formatter { get; }

        public new InputObjectType DeclaringType =>
            (InputObjectType)base.DeclaringType;

        public override Type RuntimeType => _runtimeType;

        /// <summary>
        /// Defines if the runtime type is represented as an <see cref="Optional{T}" />.
        /// </summary>
        internal bool IsOptional { get; private set; }

        protected internal PropertyInfo? Property { get; }

        protected override void OnCompleteField(
            ITypeCompletionContext context,
            ITypeSystemMember declaringMember,
            InputFieldDefinition definition)
        {
            base.OnCompleteField(context, declaringMember, definition);

            _runtimeType = definition.Property?.PropertyType ?? typeof(object);

            if (_runtimeType is { IsGenericType: true } &&
                _runtimeType.GetGenericTypeDefinition() == typeof(Optional<>))
            {
                IsOptional = true;
                _runtimeType = _runtimeType.GetGenericArguments()[0];
            }

            DefaultValue = FieldInitHelper.CreateDefaultValue(
                context, definition, Type, Coordinate);
        }
    }
}
