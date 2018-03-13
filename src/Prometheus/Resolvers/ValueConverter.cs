using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Prometheus.Abstractions;

namespace Prometheus.Resolvers
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

        public static IValue Convert(object value, ISchemaDocument schema, IType desiredType)
        {
            if (schema == null)
            {
                throw new ArgumentNullException(nameof(schema));
            }

            if (desiredType == null)
            {
                throw new ArgumentNullException(nameof(desiredType));
            }

            if (desiredType.IsNonNullType() && value == null)
            {
                throw new GraphQLQueryException("The desired type is a non null type.");
            }

            if (value == null)
            {
                return NullValue.Instance;
            }

            if (desiredType.IsScalarType())
            {
                return _scalarConverter.Convert(value, schema, desiredType);
            }

            if (schema.EnumTypes.ContainsKey(desiredType.TypeName()))
            {
                return _enumValueConverter.Convert(value, schema, desiredType);
            }

            if (desiredType.IsListType())
            {
                return _listValueConverter.Convert(value, schema, desiredType);
            }

            return _inputObjectConverter.Convert(value, schema, desiredType);
        }
    }
}