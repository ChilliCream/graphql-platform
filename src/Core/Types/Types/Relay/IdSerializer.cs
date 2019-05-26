using System.Buffers;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Relay
{
    public sealed class IdSerializer
    {
        private const int _stackallocThreshold = 256;
        private const int _divisor = 4;
        private const byte _separator = (byte)'-';
        private const byte _string = (byte)'x';
        private const byte _guid = (byte)'g';
        private const byte _short = (byte)'s';
        private const byte _int = (byte)'i';
        private const byte _long = (byte)'l';
        private const byte _default = (byte)'d';
        private const char _forwardSlash = '/';
        private const char _equals = '=';

        private static readonly Encoding _utf8 = Encoding.UTF8;

        public string Serialize(NameString typeName, object id)
        {
            if (id == null)
            {
                throw new ArgumentNullException(nameof(id));
            }

            typeName.EnsureNotEmpty("typeName");

            byte[] serializedIdArray = null;
            (byte type, byte[] value) serializedId = SerializeId(id);
            int length = typeName.Value.Length + serializedId.value.Length + 2;
            bool useStackalloc = length <= _stackallocThreshold;
            // TODO : we have to first reimplemet the base 64 algorithm in order
            // to take advantage of span.
            // Span<byte> serializedIdSpan = useStackalloc
            //     ? stackalloc byte[length]
            //     : (serializedIdArray = ArrayPool<byte>.Shared.Rent(length));
            serializedIdArray = ArrayPool<byte>.Shared.Rent(length);
            //serializedIdSpan = serializedIdSpan.Slice(0, length);
            Span<byte> serializedIdSpan = serializedIdArray.AsSpan();

            int index = 0;
            for (int i = 0; i < typeName.Value.Length; i++)
            {
                serializedIdSpan[index++] = (byte)typeName.Value[i];
            }

            serializedIdSpan[index++] = _separator;
            serializedIdSpan[index++] = serializedId.type;

            for (int i = 0; i < serializedId.value.Length; i++)
            {
                serializedIdSpan[index++] = (byte)serializedId.value[i];
            }

            string value = Convert.ToBase64String(serializedIdArray, 0, length);

            if (serializedIdArray != null)
            {
                serializedIdSpan.Clear();
                ArrayPool<byte>.Shared.Return(serializedIdArray);
            }

            return value;
        }

        public IdValue Deserialize(string serializedId)
        {
            if (serializedId == null)
            {
                throw new ArgumentNullException(nameof(serializedId));
            }

            ReadOnlySpan<byte> raw = Convert.FromBase64String(serializedId);
            int separatorIndex = FindSeparator(in raw);

            string typeName = ToString(raw.Slice(0, separatorIndex).ToArray());

            object value = DeserializeId(
                raw.Slice(separatorIndex + 1, 1),
                raw.Slice(separatorIndex + 2));

            return new IdValue(typeName, value);
        }

        private static unsafe string ToString(
            ReadOnlySpan<byte> unescapedValue)
        {
            fixed (byte* bytePtr = unescapedValue)
            {
                return _utf8.GetString(bytePtr, unescapedValue.Length);
            }
        }

        public static bool IsPossibleBase64String(string s)
        {
            if (s == null)
            {
                throw new ArgumentNullException(nameof(s));
            }

            if (s.Length % _divisor != 0)
            {
                return false;
            }

            int equalsCount = 0;

            for (int i = 0; i < s.Length; i++)
            {
                if (IsBase64Char(s[i]))
                {
                    if (equalsCount > 0)
                    {
                        return false;
                    }
                }
                else if (s[i] == _equals)
                {
                    equalsCount++;
                }
            }

            return equalsCount == 0 || equalsCount % _divisor > 0;
        }

        private static bool IsBase64Char(in char c)
        {
            return c.IsLetter()
                || c.IsDigit()
                || c.IsPlus()
                || c == _forwardSlash;
        }


        private static (byte, byte[]) SerializeId(object result)
        {
            switch (result)
            {
                case string s:
                    return (_string, _utf8.GetBytes(s));
                case Guid g:
                    return (_guid, g.ToByteArray());
                case short s:
                    return (_short, BitConverter.GetBytes(s));
                case int i:
                    return (_int, BitConverter.GetBytes(i));
                case long l:
                    return (_long, BitConverter.GetBytes(l));
                default:
                    return (_default, _utf8.GetBytes(result.ToString()));
            }
        }

        private static object DeserializeId(
            in ReadOnlySpan<byte> type,
            in ReadOnlySpan<byte> value)
        {
            switch (type[0])
            {
                case _guid:
                    return new Guid(value.ToArray());
                case _short:
                    return BitConverter.ToInt16(value.ToArray(), 0);
                case _int:
                    return BitConverter.ToInt32(value.ToArray(), 0);
                case _long:
                    return BitConverter.ToInt64(value.ToArray(), 0);
                default:
                    return ToString(value);
            }
        }

        private static int FindSeparator(in ReadOnlySpan<byte> serializedId)
        {
            for (int i = 0; i < serializedId.Length; i++)
            {
                if (serializedId[i] == _separator)
                {
                    return i;
                }
            }
            return -1;
        }
    }
}
