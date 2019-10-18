using System;
using HotChocolate.Language;
using HotChocolate.Types;

namespace StrawberryShake.Generators
{
    internal class CustomScalarType
        : ScalarType
    {
        public CustomScalarType(ScalarTypeDefinitionNode t)
            : base(t.Name.Value)
        {
            Description = t.Description?.Value;
            SyntaxNode = t;
        }

        public override Type ClrType => typeof(object);

        public ScalarTypeDefinitionNode SyntaxNode { get; }

        public override bool IsInstanceOfType(IValueNode literal)
        {
            throw new NotSupportedException();
        }

        public override object ParseLiteral(IValueNode literal)
        {
            throw new NotSupportedException();
        }

        public override IValueNode ParseValue(object value)
        {
            throw new NotSupportedException();
        }

        public override bool TryDeserialize(object serialized, out object value)
        {
            throw new NotSupportedException();
        }

        public override bool TrySerialize(object value, out object serialized)
        {
            throw new NotSupportedException();
        }
    }
}
