using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types
{
    public class OutputFieldBase<TDefinition>
        : FieldBase<IOutputType, TDefinition>
        , IOutputField
        where TDefinition : OutputFieldDefinitionBase
    {
        internal OutputFieldBase(TDefinition definition, bool sortArgumentsByName)
            : base(definition)
        {
            SyntaxNode = definition.SyntaxNode;
            Arguments = new FieldCollection<Argument>(
                definition.Arguments.Select(t => new Argument(t)),
                sortArgumentsByName);
            IsDeprecated = !string.IsNullOrEmpty(
                definition.DeprecationReason);
            DeprecationReason = definition.DeprecationReason;
        }

        public new IComplexOutputType DeclaringType =>
            (IComplexOutputType)base.DeclaringType;

        public FieldDefinitionNode SyntaxNode { get; }

        public FieldCollection<Argument> Arguments { get; }

        IFieldCollection<IInputField> IOutputField.Arguments => Arguments;

        /// <summary>
        /// Defines if this field as a introspection field.
        /// </summary>
        public virtual bool IsIntrospectionField { get; } = false;

        public bool IsDeprecated { get; }

        public string DeprecationReason { get; }

        protected override void OnCompleteField(
            ITypeCompletionContext context,
            TDefinition definition)
        {
            base.OnCompleteField(context, definition);

            foreach (Argument argument in Arguments)
            {
                argument.CompleteField(context);
            }
        }
    }
}
