using System;
using HotChocolate.Properties;

namespace HotChocolate.Utilities
{
    public abstract class TypeConverter<TFrom, TTo>
        : ITypeConverter
    {
        public Type From => typeof(TFrom);

        public Type To => typeof(TTo);

        public abstract TTo Convert(TFrom from);

        public object Convert(object source)
        {
            if (source is TFrom from)
            {
                return Convert(from);
            }

            throw new NotSupportedException(
                string.Format(
                    TypeResources.TypeConvertion_ConvertNotSupported,
                    From.Name,
                    To.Name));
        }
    }
}
