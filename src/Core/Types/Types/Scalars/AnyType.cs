using System;
using HotChocolate.Language;
using HotChocolate.Utilities;

namespace HotChocolate.Types
{
    public sealed class AnyType
        : ScalarType
    {
        private readonly ObjectValueToDictionaryConverter _converter =
            new ObjectValueToDictionaryConverter();

        public AnyType()
            : base("Any")
        {

        }

        public override Type ClrType => typeof(object);

        public override bool IsInstanceOfType(IValueNode literal)
        {
            switch (literal)
            {
                case StringValueNode svn:
                case IntValueNode ivn:
                case FloatValueNode fvn:
                case BooleanValueNode bvn:
                case ListValueNode lvn:
                case ObjectValueNode ovn:
                    return true;

                default:
                    return false;
            }
        }

        public override object ParseLiteral(IValueNode literal)
        {
            switch (literal)
            {
                case StringValueNode svn:
                    return svn.Value;

                case IntValueNode ivn:
                    return long.Parse(ivn.Value);

                case FloatValueNode fvn:
                    return decimal.Parse(fvn.Value);

                case BooleanValueNode bvn:
                    return bvn.Value;

                case ListValueNode lvn:
                    return _converter.Convert(lvn);

                case ObjectValueNode ovn:
                    return _converter.Convert(ovn);

                default:
                    return false;
            }
        }

        public override IValueNode ParseValue(object value)
        {
            throw new NotImplementedException();
        }

        public override object Serialize(object value)
        {
            throw new NotImplementedException();
        }

        public override bool TryDeserialize(object serialized, out object value)
        {
            throw new NotImplementedException();
        }
    }
}
