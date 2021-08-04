using System.Collections.Generic;
using System.Globalization;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Types.Descriptors.Definitions;

#nullable enable

namespace HotChocolate.Types
{
    /// <summary>
    /// Represents a field or directive argument.
    /// </summary>
    public class Argument : FieldBase<IInputType, ArgumentDefinition>, IInputField
    {
        public Argument(ArgumentDefinition definition, int index)
            : base(definition, index)
        {
            DefaultValue = definition.DefaultValue;

            IReadOnlyList<IInputValueFormatter> formatters = definition.GetFormatters();

            if (formatters.Count == 0)
            {
                Formatter = null;
            }
            else if (formatters.Count == 1)
            {
                Formatter = formatters[0];
            }
            else
            {
                Formatter = new AggregateInputValueFormatter(formatters);
            }

            DeclaringMember = default!;
        }

        /// <summary>
        /// The associated syntax node from the GraphQL SDL.
        /// </summary>
        public new InputValueDefinitionNode? SyntaxNode
            => (InputValueDefinitionNode?)base.SyntaxNode;

        /// <summary>
        /// Gets the type system member that declares this argument.
        /// </summary>
        public ITypeSystemMember DeclaringMember { get; private set; }

        /// <inheritdoc />
        public IValueNode? DefaultValue { get; private set; }

        /// <inheritdoc />
        public IInputValueFormatter? Formatter { get; }

        protected override void OnCompleteField(
            ITypeCompletionContext context,
            ITypeSystemMember declaringMember,
            ArgumentDefinition definition)
        {
            if (definition.Type is null)
            {
                context.ReportError(SchemaErrorBuilder.New()
                    .SetMessage(string.Format(
                        CultureInfo.InvariantCulture,
                        TypeResources.Argument_TypeIsNull,
                        definition.Name))
                    .SetTypeSystemObject(context.Type)
                    .Build());
                return;
            }

            base.OnCompleteField(context, declaringMember, definition);
            DefaultValue = FieldInitHelper.CreateDefaultValue(
                context, definition, Type, Coordinate);
            DeclaringMember = declaringMember;
        }
    }
}
