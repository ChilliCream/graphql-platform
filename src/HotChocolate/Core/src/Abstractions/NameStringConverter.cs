using System;
using System.ComponentModel;
using System.Globalization;

namespace HotChocolate
{
    internal class NameStringConverter : TypeConverter
    {
        public override bool CanConvertFrom(
            ITypeDescriptorContext context,
            Type sourceType) =>
            sourceType == typeof(string)
            || base.CanConvertFrom(context, sourceType);

        public override object ConvertFrom(
            ITypeDescriptorContext context,
            CultureInfo culture,
            object value) =>
            value is string s
                ? NameString.ConvertFromString(s)
                : base.ConvertFrom(context, culture, value);

        public override object ConvertTo(
            ITypeDescriptorContext context,
            CultureInfo culture,
            object value,
            Type destinationType) =>
            destinationType == typeof(string)
                ? value.ToString()
                : base.ConvertTo(context, culture, value, destinationType);
    }
}
