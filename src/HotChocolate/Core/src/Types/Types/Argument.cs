using System;
using System.Globalization;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Types.Descriptors.Definitions;

#nullable enable

namespace HotChocolate.Types
{
    public class Argument
        : FieldBase<IInputType, ArgumentDefinition>
        , IInputField
    {
        public Argument(
            ArgumentDefinition definition)
            : base(definition)
        {
            SyntaxNode = definition.SyntaxNode;
            DefaultValue = definition.DefaultValue;
        }

        public InputValueDefinitionNode? SyntaxNode { get; }

        public IValueNode? DefaultValue { get; private set; }

        protected override void OnCompleteField(
            ICompletionContext context,
            ArgumentDefinition definition)
        {
            if (definition.Type == null)
            {
                context.ReportError(SchemaErrorBuilder.New()
                    .SetMessage(string.Format(
                        CultureInfo.InvariantCulture,
                        TypeResources.Argument_TypeIsNull,
                        definition.Name))
                    .SetTypeSystemObject(context.Type)
                    .Build());
            }
            else
            {
                base.OnCompleteField(context, definition);
                DefaultValue = FieldInitHelper.CreateDefaultValue(context, definition, Type);
            }
        }
    }
}
