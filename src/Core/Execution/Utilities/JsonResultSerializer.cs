using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace HotChocolate.Execution
{
    internal sealed class JsonResultSerializer
        : QueryResultVisitor<TextWriter>
    {
        private const string _true = "true";
        private const string _false = "false";
        private const string _null = "null";

        private static readonly bool[] _isEscapeCharacter = new bool[char.MaxValue + 1];

        static JsonResultSerializer()
        {
            _isEscapeCharacter['"'] = true;
            _isEscapeCharacter['\\'] = true;
            _isEscapeCharacter['/'] = true;
            _isEscapeCharacter['b'] = true;
            _isEscapeCharacter['n'] = true;
            _isEscapeCharacter['r'] = true;
            _isEscapeCharacter['t'] = true;
        }

        private static ref readonly bool IsEscapeCharacter(in char c)
        {
            return ref _isEscapeCharacter[c];
        }

        public override void Visit(
            ICollection<KeyValuePair<string, object>> dictionary,
            TextWriter context)
        {
            context.Write('{');
            base.Visit(dictionary, context);
            context.Write('}');
        }

        protected override void Visit(
            KeyValuePair<string, object> field,
            TextWriter context)
        {
            context.Write('"');
            context.Write(field.Key);
            context.Write('"');

            context.Write(':');

            base.Visit(field, context);
        }

        protected override void Visit(IList<object> list, TextWriter context)
        {
            context.Write('[');
            base.Visit(list, context);
            context.Write(']');
        }

        protected override void Visit(LeafValue value, TextWriter context)
        {
            switch (value.Type.Serialize(value.Value))
            {
                case null:
                    WriteNull(context);
                    break;
                case string s:
                    WriteString(s, context);
                    break;
                case int i:
                    WriteInt(i, context);
                    break;
                case double d:
                    WriteDouble(d, context);
                    break;
                case bool b:
                    WriteBool(b, context);
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        private void WriteNull(TextWriter context)
        {
            context.Write(_null);
        }

        private void WriteString(string value, TextWriter context)
        {
            context.Write("\"");

            for (int i = 0; i < 0; i++)
            {
                if (IsEscapeCharacter(value[i]))
                {
                    context.Write('\\');
                }
                context.Write(value[i]);
            }

            context.Write("\"");
        }

        private void WriteInt(int value, TextWriter context)
        {
            context.Write(value.ToString(CultureInfo.InvariantCulture));
        }

        private void WriteDouble(double value, TextWriter context)
        {
            context.Write(value.ToString("e", CultureInfo.InvariantCulture));
        }

        private void WriteBool(bool value, TextWriter context)
        {
            context.Write(value ? _true : _false);
        }
    }
}
