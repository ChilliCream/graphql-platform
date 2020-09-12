using System;
using System.Collections.Generic;
using System.Globalization;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Utilities;

#nullable enable

namespace HotChocolate.Types
{
    public sealed class AnyType
        : ScalarType
    {
        private readonly ObjectValueToDictionaryConverter _objectValueToDictConverter =
            new ObjectValueToDictionaryConverter();
        private ObjectToDictionaryConverter _objectToDictConverter = default!;
        private ITypeConverter _converter = default!;

        public AnyType()
            : base(ScalarNames.Any, BindingBehavior.Explicit)
        {
        }

        public AnyType(NameString name)
            : base(name, BindingBehavior.Explicit)
        {
        }

        public AnyType(NameString name, string description)
            : base(name, BindingBehavior.Explicit)
        {
            Description = description;
        }

        public override Type RuntimeType => typeof(object);

        protected override void OnCompleteType(
            ITypeCompletionContext context,
            IDictionary<string, object?> contextData)
        {
            _converter = context.Services.GetTypeConverter();
            _objectToDictConverter = new ObjectToDictionaryConverter(_converter);
            base.OnCompleteType(context, contextData);
        }

        public override bool IsInstanceOfType(IValueNode literal)
        {
            if (literal is null)
            {
                throw new ArgumentNullException(nameof(literal));
            }

            switch (literal)
            {
                case StringValueNode _:
                case IntValueNode _:
                case FloatValueNode _:
                case BooleanValueNode _:
                case ListValueNode _:
                case ObjectValueNode _:
                case NullValueNode _:
                    return true;

                default:
                    return false;
            }
        }

        public override object? ParseLiteral(IValueNode literal, bool withDefaults = true)
        {
            switch (literal)
            {
                case StringValueNode svn:
                    return svn.Value;

                case IntValueNode ivn:
                    return long.Parse(ivn.Value, CultureInfo.InvariantCulture);

                case FloatValueNode fvn:
                    return decimal.Parse(fvn.Value, CultureInfo.InvariantCulture);

                case BooleanValueNode bvn:
                    return bvn.Value;

                case ListValueNode lvn:
                    return _objectValueToDictConverter.Convert(lvn);

                case ObjectValueNode ovn:
                    return _objectValueToDictConverter.Convert(ovn);

                case NullValueNode _:
                    return null;

                default:
                    throw new SerializationException(
                        TypeResourceHelper.Scalar_Cannot_ParseLiteral(Name, literal.GetType()),
                        this);
            }
        }

        public override IValueNode ParseValue(object? value)
        {
            if (value is null)
            {
                return NullValueNode.Default;
            }

            return ParseValue(value, new HashSet<object>());
        }

        private IValueNode ParseValue(object? value, ISet<object> set)
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
                case int i:
                    return new IntValueNode(i);
                case long l:
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
                type, typeof(string), value, out object? converted)
                && converted is string c)
            {
                return new StringValueNode(c);
            }

            if (set.Add(value))
            {
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

                if (value is IReadOnlyList<object> list)
                {
                    var valueList = new List<IValueNode>();
                    foreach (object element in list)
                    {
                        valueList.Add(ParseValue(element, set));
                    }
                    return new ListValueNode(valueList);
                }

                return ParseValue(_objectToDictConverter.Convert(value), set);
            }

            // TODO : resources
            throw new SerializationException(
                "Cycle in object graph detected.",
                this);
        }

        public override IValueNode ParseResult(object? resultValue) =>
            ParseValue(resultValue);

        public override bool TrySerialize(object? runtimeValue, out object? resultValue)
        {
            if (runtimeValue is null)
            {
                resultValue = null;
                return true;
            }

            switch (runtimeValue)
            {
                case string _:
                case short _:
                case int _:
                case long _:
                case float _:
                case double _:
                case decimal _:
                case bool _:
                    resultValue = runtimeValue;
                    return true;

                default:
                    Type type = runtimeValue.GetType();

                    if (type.IsValueType && 
                        _converter.TryConvert(type, typeof(string), runtimeValue, out object? c) && 
                        c is string casted)
                    {
                        resultValue = casted;
                        return true;
                    }

                    resultValue = _objectToDictConverter.Convert(runtimeValue);
                    return true;
            }
        }

        public override bool TryDeserialize(object? resultValue, out object? runtimeValue)
        {
            runtimeValue = resultValue;
            return true;
        }
    }
}
