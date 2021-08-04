using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;

#nullable enable

namespace HotChocolate.Types
{
    public class OutputFieldBase<TDefinition>
        : FieldBase<IOutputType, TDefinition>
        , IOutputField
        where TDefinition : OutputFieldDefinitionBase
    {
        internal OutputFieldBase(TDefinition definition, int index) : base(definition, index)
        {
            IsDeprecated = !string.IsNullOrEmpty(definition.DeprecationReason);
            DeprecationReason = definition.DeprecationReason;
            Arguments = default!;
        }

        /// <inheritdoc />
        public new IComplexOutputType DeclaringType => (IComplexOutputType)base.DeclaringType;

        public new FieldDefinitionNode? SyntaxNode => (FieldDefinitionNode?)base.SyntaxNode;

        public FieldCollection<Argument> Arguments { get; private set; }

        IFieldCollection<IInputField> IOutputField.Arguments => Arguments;

        /// <summary>
        /// Defines if this field as a introspection field.
        /// </summary>
        public virtual bool IsIntrospectionField => false;

        /// <inheritdoc />
        public bool IsDeprecated { get; }

        /// <inheritdoc />
        public string? DeprecationReason { get; }

        protected override void OnCompleteField(
            ITypeCompletionContext context,
            ITypeSystemMember declaringMember,
            TDefinition definition)
        {
            base.OnCompleteField(context, declaringMember, definition);

            Arguments = FieldCollection<Argument>.From(
                definition
                    .GetArguments()
                    .Where(t => !t.Ignore)
                    .Select(t => new Argument(t, Coordinate.With(argumentName: t.Name))),
                context.DescriptorContext.Options.SortFieldsByName);

            foreach (Argument argument in Arguments)
            {
                argument.CompleteField(context);
            }
        }

        protected virtual void OnCompleteFields(
            ITypeCompletionContext context,
            TDefinition definition,
            ref Argument[] fields)
        {
            IEnumerable<ArgumentDefinition> fieldDefs = definition.Arguments.Where(t => !t.Ignore);

            if (context.DescriptorContext.Options.SortFieldsByName)
            {
                fieldDefs = fieldDefs.OrderBy(t => t.Name);
            }

            var index = 0;
            foreach (var fieldDefinition in fieldDefs)
            {
                fields[index] = new(fieldDefinition, index);
                index++;
            }

            if (fields.Length > index)
            {
                Array.Resize(ref fields, index);
            }
        }
    }
}
