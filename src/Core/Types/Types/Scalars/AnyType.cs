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
            if (value is null)
            {
                return NullValueNode.Default;
            }

            switch (value)
            {
                case string s:
                    return new StringValueNode(s);
                case short s:
                    return new IntValueNode(s);
                case ushort s:
                    return new IntValueNode(s);
                case int i:
                    return new IntValueNode(i);
                case uint i:
                    return new IntValueNode(i);
                case long l:
                    return new IntValueNode(l);
                case ulong l:
                    return new IntValueNode(l);
                case float f:
                    return new FloatValueNode(f);
                case double d:
                    return new FloatValueNode(d);
                case decimal d:
                    return new FloatValueNode(d);
                case bool b:
                    return new BooleanValueNode(b);
            }

            throw new  NotImplementedException();
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
