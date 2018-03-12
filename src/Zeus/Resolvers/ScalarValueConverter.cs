using System;
using Zeus.Abstractions;

namespace Zeus.Resolvers
{
    internal class ScalarValueConverter
        : IValueConverter
    {
        public object Convert(IValue value, Type desiredType)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (value is IScalarValue sv)
            {
                return System.Convert.ChangeType(sv.Value, desiredType);
            }

            throw new ArgumentException(
                "The specified type is not a sacalar type.",
                nameof(value));
        }

        public IValue Convert(object value, ISchemaDocument schema, IType desiredType)
        {
            if (schema == null)
            {
                throw new ArgumentNullException(nameof(schema));
            }

            if (desiredType == null)
            {
                throw new ArgumentNullException(nameof(desiredType));
            }

            if (value == null)
            {
                return NullValue.Instance;
            }

            if (NamedType.Boolean.Equals(desiredType.InnerType()))
            {
                return new BooleanValue((bool)value);
            }

            if (NamedType.Float.Equals(desiredType.InnerType()))
            {
                return new FloatValue((decimal)value);
            }

            if (NamedType.Integer.Equals(desiredType.InnerType()))
            {
                return new IntegerValue((int)value);
            }

            if (NamedType.String.Equals(desiredType.InnerType()))
            {
                return new StringValue(value is string s ? s : value.ToString());
            }

            throw new NotSupportedException();
        }
    }
}