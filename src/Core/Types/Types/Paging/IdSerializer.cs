using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Paging
{
    internal sealed class IdSerializer
    {
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

        private readonly ConcurrentDictionary<string, byte[]> _typeNames =
            new ConcurrentDictionary<string, byte[]>();

        public string Serialize(NameString typeName, object id)
        {
            if (id == null)
            {
                throw new ArgumentNullException(nameof(id));
            }

            typeName.EnsureNotEmpty("typeName");

            return ToBase64String(
                SerializeTypeName(typeName),
                SerializeId(id));
        }

        public IdValue Deserialize(string serializedId)
        {
            if (serializedId == null)
            {
                throw new ArgumentNullException(nameof(serializedId));
            }

            ReadOnlySpan<byte> raw = Convert.FromBase64String(serializedId);
            int separatorIndex = FindSeparator(in raw);

            string typeName = _utf8.GetString(
                raw.Slice(0, separatorIndex).ToArray());

            object value = DeserializeId(
                raw.Slice(separatorIndex + 1, 1),
                raw.Slice(separatorIndex + 2));

            return new IdValue(typeName, value);
        }

        public static bool IsPossibleBase64String(string s)
        {
            if (s == null)
            {
                throw new ArgumentNullException(nameof(s));
            }

            if (s.Length % 4 != 0)
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
            return equalsCount == 0 || equalsCount % 4 > 0;
        }

        private static bool IsBase64Char(in char c)
        {
            return c.IsLetter()
                || c.IsDigit()
                || c.IsPlus()
                || c == _forwardSlash;
        }

        private string ToBase64String(
            byte[] typeName,
            (byte type, byte[] value) id)
        {
            using (var stream = new MemoryStream())
            {
                stream.Write(typeName, 0, typeName.Length);
                stream.WriteByte(_separator);
                stream.WriteByte(id.type);
                stream.Write(id.value, 0, id.value.Length);
                return Convert.ToBase64String(stream.ToArray());
            }
        }

        private (byte, byte[]) SerializeId(object result)
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

        private object DeserializeId(
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
                    return _utf8.GetString(value.ToArray(), 0, value.Length);
            }
        }

        private byte[] SerializeTypeName(string typeName)
        {
            if (!_typeNames.TryGetValue(typeName,
                out byte[] serialized))
            {
                serialized = _utf8.GetBytes(typeName);
                _typeNames.TryAdd(typeName, serialized);
            }
            return serialized;
        }

        private int FindSeparator(in ReadOnlySpan<byte> serializedId)
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
