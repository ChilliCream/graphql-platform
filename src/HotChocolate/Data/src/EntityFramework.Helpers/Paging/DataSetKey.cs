using System.Buffers.Text;
using System.Reflection;
using System.Text;

namespace HotChocolate.Data;

internal sealed class DataSetKey
{
    public DataSetKey(PropertyInfo property, bool ascending = true)
    {
        Property = property;
        CompareMethod = Property.PropertyType.GetMethod("CompareTo", [Property.PropertyType,])!;
        Ascending = ascending;
    }

    public bool Ascending { get; set; }

    public PropertyInfo Property { get; }

    public MethodInfo CompareMethod { get; }

    public object Parse(string cursorValue)
    {
        if (typeof(string) == Property.PropertyType)
        {
            return cursorValue;
        }

        if (typeof(int) == Property.PropertyType)
        {
            return int.Parse(cursorValue);
        }

        throw new NotSupportedException();
    }

    public object Parse(ReadOnlySpan<byte> cursorValue)
    {
        if (typeof(string) == Property.PropertyType)
        {
            return Encoding.UTF8.GetString(cursorValue);
        }

        if (typeof(short) == Property.PropertyType)
        {
            if (!Utf8Parser.TryParse(cursorValue, out short value, out _))
            {
                throw new FormatException("The cursor value is not a valid short.");
            }

            return value;
        }
        
        if (typeof(int) == Property.PropertyType)
        {
            if (!Utf8Parser.TryParse(cursorValue, out int value, out _))
            {
                throw new FormatException("The cursor value is not a valid integer.");
            }

            return value;
        }
        
        if (typeof(long) == Property.PropertyType)
        {
            if (!Utf8Parser.TryParse(cursorValue, out long value, out _))
            {
                throw new FormatException("The cursor value is not a valid long.");
            }

            return value;
        }
        
        
        if (typeof(Guid) == Property.PropertyType)
        {
            if (!Utf8Parser.TryParse(cursorValue, out Guid value, out _))
            {
                throw new FormatException("The cursor value is not a valid guid.");
            }

            return value;
        }

        throw new NotSupportedException();
    }

    public bool TryFormat(object instance, Span<byte> span, out int written)
    {
        if (typeof(string) == Property.PropertyType)
        {
            var data = ((string)Property.GetValue(instance)!).AsSpan();
#if NET8_0_OR_GREATER
            return Encoding.UTF8.TryGetBytes(data, span, out written);
#else
            written = Encoding.UTF8.GetBytes(data, span);
            return true;
#endif
        }

        if (typeof(short) == Property.PropertyType)
        {
            var data = (short)Property.GetValue(instance)!;
            return Utf8Formatter.TryFormat(data, span, out written);
        }
        
        if (typeof(int) == Property.PropertyType)
        {
            var data = (int)Property.GetValue(instance)!;
            return Utf8Formatter.TryFormat(data, span, out written);
        }
        
        if (typeof(long) == Property.PropertyType)
        {
            var data = (long)Property.GetValue(instance)!;
            return Utf8Formatter.TryFormat(data, span, out written);
        }
        
        if (typeof(Guid) == Property.PropertyType)
        {
            var data = (Guid)Property.GetValue(instance)!;
            return Utf8Formatter.TryFormat(data, span, out written);
        }

        throw new NotSupportedException();
    }
}