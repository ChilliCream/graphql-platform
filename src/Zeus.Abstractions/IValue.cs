using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Zeus.Abstractions
{
    public interface IValue
    {

    }

    public sealed class NullValue
        : IValue
    {
        private NullValue() { }

        public override string ToString()
        {
            return "null";
        }

        public static NullValue Instance { get; } = new NullValue();
    }

    public class ScalarValue<TValue>
        : IValue
    {
        protected ScalarValue(TValue value)
        {
            if (object.Equals(value, null))
            {
                throw new ArgumentNullException(nameof(value));
            }

            Value = value;
        }

        public TValue Value { get; }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(Value); // TODO : get rid of newtonsoft in abstractions
        }
    }

    public sealed class IntegerValue
       : ScalarValue<int>
    {
        public IntegerValue(int value)
            : base(value)
        {
        }
    }

    public sealed class FloatValue
        : ScalarValue<decimal>
    {
        public FloatValue(decimal value)
            : base(value)
        {
        }
    }

    public sealed class EnumValue
       : ScalarValue<string>
    {
        public EnumValue(string value)
            : base(value)
        {
        }
    }

    public class BooleanValue
        : ScalarValue<bool>
    {
        public BooleanValue(bool value)
            : base(value)
        {
        }
    }

    public sealed class StringValue
       : ScalarValue<string>
    {
        public StringValue(string value)
            : base(value)
        {
        }
    }

    public sealed class ListValue
        : IValue
    {
        public ListValue(IEnumerable<IValue> items)
        {
            Items = items.ToArray();
        }

        public IReadOnlyCollection<IValue> Items { get; }

        public override string ToString()
        {
            return "[" + string.Join(", ", Items.Select(t => t.ToString())) + "]";
        }
    }

    public sealed class Variable
        : IValue
    {
        public Variable(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            Name = name;
        }

        public string Name { get; }

        public override string ToString()
        {
            return "$" + Name;
        }
    }

    public sealed class InputObjectValue
        : IValue
    {
        public InputObjectValue(IReadOnlyDictionary<string, IValue> fields)
        {
            if (fields == null)
            {
                throw new ArgumentNullException(nameof(fields));
            }

            Fields = fields;
        }

        public IReadOnlyDictionary<string, IValue> Fields { get; }

        public override string ToString()
        {
            return "{" + string.Join(", ", Fields.Select(t => t.Key + ": " + t.Value)) + "}";
        }
    }

    public static class ValueConverter
    {
        public static T ConvertTo<T>(IValue value)
        {
            throw new NotImplementedException();
        }
    }
}