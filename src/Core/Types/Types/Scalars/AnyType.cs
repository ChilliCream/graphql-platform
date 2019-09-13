using System;
using System.Collections.Generic;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Utilities;

namespace HotChocolate.Types
{
    public sealed class AnyType
        : ScalarType
    {
        private readonly ObjectValueToDictionaryConverter _objectValueToDictConverter =
            new ObjectValueToDictionaryConverter();
        private ObjectToDictionaryConverter _objectToDictConverter;
        private ITypeConversion _converter;

        public AnyType()
            : base("Any")
        {
        }

        public override Type ClrType => typeof(object);

        protected override void OnCompleteType(
            ICompletionContext context,
            IDictionary<string, object> contextData)
        {
            _converter = context.Services.GetTypeConversion();
            _objectToDictConverter = new ObjectToDictionaryConverter(_converter);
            base.OnCompleteType(context, contextData);
        }

        public override bool IsInstanceOfType(IValueNode literal)
        {
            switch (literal)
            {
                case StringValueNode svn:
                case IntValueNode ivn:
                case FloatValueNode fvn:
                case BooleanValueNode bvn:
                case ListValueNode lvn:
                case ObjectValueNode ovn:
                    return true;

                default:
                    return false;
            }
        }

        public override object ParseLiteral(IValueNode literal)
        {
            switch (literal)
            {
                case StringValueNode svn:
                    return svn.Value;

                case IntValueNode ivn:
                    return long.Parse(ivn.Value);

                case FloatValueNode fvn:
                    return decimal.Parse(fvn.Value);

                case BooleanValueNode bvn:
                    return bvn.Value;

                case ListValueNode lvn:
                    return _objectValueToDictConverter.Convert(lvn);

                case ObjectValueNode ovn:
                    return _objectValueToDictConverter.Convert(ovn);

                default:
                    return false;
            }
        }

        public override IValueNode ParseValue(object value)
        {
            if (value is null)
            {
                return NullValueNode.Default;
            }
            return ParseValue(value, new HashSet<object>());
        }

        private IValueNode ParseValue(object value, ISet<object> set)
        {
            if (value is null)
            {
                return NullValueNode.Default;
            }

            switch (value)
            {
                case string s:
                    return new StringValueNode(s);
                case short s:
                    return new IntValueNode(s);
                case ushort s:
                    return new IntValueNode(s);
                case int i:
                    return new IntValueNode(i);
                case uint i:
                    return new IntValueNode(i);
                case long l:
                    return new IntValueNode(l);
                case ulong l:
                    return new IntValueNode(l);
                case float f:
                    return new FloatValueNode(f);
                case double d:
                    return new FloatValueNode(d);
                case decimal d:
                    return new FloatValueNode(d);
                case bool b:
                    return new BooleanValueNode(b);
            }

            Type type = value.GetType();

            if (type.IsValueType && _converter.TryConvert(
                type, typeof(string), value, out object converted)
                && converted is string c)
            {
                return new StringValueNode(c);
            }

            if (set.Contains(value))
            {
                if (value is IReadOnlyList<object> list)
                {
                    var valueList = new List<IValueNode>();
                    foreach (object element in list)
                    {
                        valueList.Add(ParseValue(element, set));
                    }
                    return new ListValueNode(valueList);
                }

                if (value is IReadOnlyDictionary<string, object> dict)
                {
                    var fields = new List<ObjectFieldNode>();
                    foreach (KeyValuePair<string, object> field in dict)
                    {
                        fields.Add(new ObjectFieldNode(
                            field.Key,
                            ParseValue(field.Value, set)));
                    }
                    return new ObjectValueNode(fields);
                }

                return ParseValue(_objectToDictConverter.Convert(value), set);
            }

            throw new ScalarSerializationException(
                "Cycle detected in graph detected.");
        }


        public override object Serialize(object value)
        {
            return _objectToDictConverter.Convert(value);
        }

        public override bool TryDeserialize(object serialized, out object value)
        {
            value = serialized;
            return true;
        }
    }
}
