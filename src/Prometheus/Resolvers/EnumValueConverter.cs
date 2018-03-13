using System;
using Prometheus.Abstractions;

namespace Prometheus.Resolvers
{
    internal class EnumValueConverter
        : IValueConverter
    {
        public object Convert(IValue value, Type desiredType)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (value is EnumValue ev)
            {
                if (typeof(string) == desiredType
                    || typeof(object) == desiredType)
                {
                    return value.Value;
                }

                if (desiredType.IsEnum)
                {
                    return System.Enum.Parse(desiredType, ev.Value, true);
                }

                return System.Convert.ChangeType(ev.Value, desiredType);
            }

            throw new ArgumentException(
                "The specified type is not an enum value type.",
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

            string s = value.ToString().ToUpperInvariant();
            if (schema.EnumTypes.TryGetValue(
                desiredType.TypeName(),
                out var typeDefinition))
            {
                if (typeDefinition.Values.Contains(s))
                {
                    return new EnumValue(s);
                }
                else
                {
                    throw new GraphQLQueryException("The specified enum does not contain the given element.");
                }
            }
            else
            {
                throw new GraphQLQueryException("The specified enum type does not exist.");
            }
        }
    }
}