
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Validation
{
    internal sealed class FieldMustBeDefinedVisitor
        : QueryVisitor
    {
        private readonly List<ValidationError> _errors =
            new List<ValidationError>();

        public FieldMustBeDefinedVisitor(ISchema schema)
            : base(schema)
        {
        }

        public IReadOnlyCollection<ValidationError> Errors => _errors;

        protected override void VisitField(
            FieldNode field,
            IType type,
            ImmutableStack<ISyntaxNode> path)
        {
            if (type is UnionType ut)
            {
                throw new NotImplementedException();
            }
        }
    }
}
