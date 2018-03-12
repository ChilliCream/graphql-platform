using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Zeus.Abstractions;

namespace Zeus.Resolvers
{
    public static class ValueConverter
    {
        private static readonly ScalarValueConverter _scalarConverter = new ScalarValueConverter();
        private static readonly EnumValueConverter _enumValueConverter = new EnumValueConverter();
        private static readonly ListValueConverter _listValueConverter = new ListValueConverter();
        private static readonly InputObjectValueConverter _inputObjectConverter = new InputObjectValueConverter();

        public static T Convert<T>(IValue value)
        {
            return (T)Convert(value, typeof(T));
        }

        public static object Convert(IValue value, Type desiredType)
        {
            if (value is NullValue)
            {
                return null;
            }

            if (value is IScalarValue)
            {
                return _scalarConverter.Convert(value, desiredType);
            }

            if (value is EnumValue)
            {
                return _enumValueConverter.Convert(value, desiredType);
            }

            if (value is ListValue)
            {
                return _listValueConverter.Convert(value, desiredType);
            }

            if (value is InputObjectValue)
            {
                return _inputObjectConverter.Convert(value, desiredType);
            }

            throw new NotSupportedException();
        }
    }
}