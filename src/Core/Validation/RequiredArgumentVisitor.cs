using System.Collections.Generic;
using System.Collections.Immutable;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Validation
{
    internal sealed class RequiredArgumentVisitor
        : QueryVisitor
    {
        private readonly List<ValidationError> _errors =
            new List<ValidationError>();

        public RequiredArgumentVisitor(ISchema schema)
            : base(schema)
        {
        }

        public IReadOnlyCollection<ValidationError> Errors => _errors;

        protected override void VisitField(
            FieldNode field,
            IType type,
            ImmutableStack<ISyntaxNode> path)
        {
            if (type is IComplexOutputType complexType)
            {
                if (complexType.Fields.ContainsField(field.Name.Value))
                {
                    foreach (InputField argument in complexType.Fields[field.Name.Value].Arguments)
                    {

                    }
                }
            }

            base.VisitField(field, type, path);
        }

        protected override void VisitDirective(
            DirectiveNode directive,
            ImmutableStack<ISyntaxNode> path)
        {
            base.VisitDirective(directive, path);
        }


    }
}
