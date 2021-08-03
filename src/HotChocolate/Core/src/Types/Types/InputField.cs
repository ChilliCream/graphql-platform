using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Utilities;

#nullable enable

namespace HotChocolate.Types
{
    public class InputField
        : FieldBase<IInputType, InputFieldDefinition>
        , IInputField
    {
        private Type _runtimeType;
        private bool _IsOptional;

        public InputField(InputFieldDefinition definition, FieldCoordinate coordinate, int index)
            : base(definition, coordinate)
        {
            Index = index;
            SyntaxNode = definition.SyntaxNode;
            DefaultValue = definition.DefaultValue;
            Property = definition.Property;

            IReadOnlyList<IInputValueFormatter> formatters = definition.GetFormatters();
            Formatter = formatters.Count switch
            {
                0 => null,
                1 => formatters[0],
                _ => new AggregateInputValueFormatter(formatters)
            };

            _runtimeType = definition.Property?.PropertyType ?? typeof(object);

            if (_runtimeType is { IsGenericType: true } &&
                _runtimeType.GetGenericTypeDefinition() == typeof(Optional<>))
            {
                _IsOptional = true;
                _runtimeType = _runtimeType.GetGenericArguments()[0];
            }
        }

        /// <summary>
        /// The associated syntax node from the GraphQL SDL.
        /// </summary>
        public InputValueDefinitionNode? SyntaxNode { get; }

        /// <inheritdoc />
        public IValueNode? DefaultValue { get; private set; }

        /// <inheritdoc />
        public IInputValueFormatter? Formatter { get; }

        public new InputObjectType DeclaringType =>
            (InputObjectType)base.DeclaringType;

        public override Type RuntimeType => _runtimeType;

        /// <summary>
        /// The position of this field in the type field list.
        /// </summary>
        internal int Index { get; }

        /// <summary>
        /// Defines if the runtime type is represented as an <see cref="Optional{T}" />.
        /// </summary>
        internal bool IsOptional => _IsOptional;

        protected internal PropertyInfo? Property { get; }

        protected override void OnCompleteField(
            ITypeCompletionContext context,
            InputFieldDefinition definition)
        {
            base.OnCompleteField(context, definition);

            _runtimeType = definition.Property?.PropertyType ?? typeof(object);

            if (_runtimeType is { IsGenericType: true } &&
                _runtimeType.GetGenericTypeDefinition() == typeof(Optional<>))
            {
                _IsOptional = true;
                _runtimeType = _runtimeType.GetGenericArguments()[0];
            }

            DefaultValue = FieldInitHelper.CreateDefaultValue(
                context, definition, Type, Coordinate);
        }
    }
}
