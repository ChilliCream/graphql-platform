using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    public abstract class NumberType<TNative, TNode>
        : ScalarType
    {
        protected NumberType(string name)
            : base(name)
        {
        }

        public override Type NativeType { get; } = typeof(TNative);

        protected virtual IEnumerable<Type> AdditionalTypes { get; } =
            Enumerable.Empty<Type>();

        public override bool IsInstanceOfType(IValueNode literal)
        {
            if (literal == null)
            {
                throw new ArgumentNullException(nameof(literal));
            }

            return literal is TNode
                   || literal is NullValueNode;
        }

        public override object ParseLiteral(IValueNode literal)
        {
            if (literal == null)
            {
                throw new ArgumentNullException(nameof(literal));
            }

            if (literal is TNode node)
            {
                return OnParseLiteral(node);
            }

            if (literal is NullValueNode)
            {
                return null;
            }

            throw new ArgumentException(
                $"The {NativeType.Name} type can only parse {typeof(TNode).Name}.");
        }

        protected abstract TNative OnParseLiteral(TNode node);

        public override IValueNode ParseValue(object value)
        {
            if (value == null)
            {
                return new NullValueNode();
            }

            if (value is TNative native)
            {
                return (IValueNode)OnParseValue(native);
            }

            if (AdditionalTypes.Any(t => value.GetType() == t))
            {
                return (IValueNode)OnParseValue(
                    (TNative)Convert.ChangeType(value, NativeType));
            }

            throw new ArgumentException(
                $"The specified value has to be an {NativeType.Name} type.");
        }

        protected abstract TNode OnParseValue(TNative value);

        public override object Serialize(object value)
        {
            if (value == null)
            {
                return null;
            }

            if (value is TNative
                || AdditionalTypes.Any(t => value.GetType() == t))
            {
                return value;
            }

            throw new ArgumentException(
                $"The specified value cannot be handled by the {Name}Type.");
        }
    }
}
