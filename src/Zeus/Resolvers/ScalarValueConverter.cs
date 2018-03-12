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
    }
}