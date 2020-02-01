using System;
using HotChocolate.Properties;

namespace HotChocolate.Utilities
{
    public sealed class DelegateTypeConverter<TFrom, TTo>
        : ITypeConverter
    {
        private readonly ChangeType<TFrom, TTo> _convert;

        public DelegateTypeConverter(ChangeType<TFrom, TTo> convert)
        {
            _convert = convert
                ?? throw new ArgumentNullException(nameof(convert));
        }

        public Type From => typeof(TFrom);

        public Type To => typeof(TTo);

        public object Convert(object source)
        {
            if (source is TFrom from)
            {
                return _convert(from);
            }

            throw new NotSupportedException(
                string.Format(
                    TypeResources.TypeConvertion_ConvertNotSupported,
                    From.Name,
                    To.Name));
        }
    }
}
