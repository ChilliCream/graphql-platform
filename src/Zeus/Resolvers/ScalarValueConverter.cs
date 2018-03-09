using System;
using Zeus.Abstractions;

namespace Zeus.Resolvers
{
    internal class ScalarValueConverter
        : IValueConverter
    {
        public T Convert<T>(IValue value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (value is IScalarValue sv)
            {
                return (T)System.Convert.ChangeType(sv.Value, typeof(T));
            }

            throw new ArgumentException(
                "The specified type is not a sacalar type.",
                nameof(value));
        }
    }
}