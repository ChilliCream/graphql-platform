using System.Collections;
using System.Globalization;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Utilities;

#nullable enable

namespace HotChocolate.Types;

public class AnyType : ScalarType
{
    private readonly ObjectValueToDictionaryConverter _objectValueToDictConverter = new();
    private ObjectToDictionaryConverter _objectToDictConverter = default!;

    /// <summary>
    /// Initializes a new instance of the <see cref="AnyType"/> class.
    /// </summary>
    public AnyType(
        string name,
        string? description = null,
        BindingBehavior bind = BindingBehavior.Explicit)
        : base(name, bind)
    {
        Description = description;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AnyType"/> class.
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public AnyType() : this(ScalarNames.Any)
    {
    }

    public override Type RuntimeType => typeof(object);

    protected override void OnCompleteType(
        ITypeCompletionContext context,
        ScalarTypeDefinition definition)
    {
        base.OnCompleteType(context, definition);
        _objectToDictConverter = new ObjectToDictionaryConverter(Converter);
    }

    public override bool IsInstanceOfType(IValueNode literal)
    {
        if (literal is null)
        {
            throw new ArgumentNullException(nameof(literal));
        }

        switch (literal)
        {
            case StringValueNode:
            case IntValueNode:
            case FloatValueNode:
            case BooleanValueNode:
            case ListValueNode:
            case ObjectValueNode:
            case NullValueNode:
                return true;

            default:
                return false;
        }
    }

    public override object? ParseLiteral(IValueNode literal)
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

            case NullValueNode:
                return null;

            default:
                throw new SerializationException(
                    TypeResourceHelper.Scalar_Cannot_ParseLiteral(Name, literal.GetType()),
                    this);
        }
    }

    public override IValueNode ParseValue(object? value)
    {
        return value is null
            ? NullValueNode.Default
            : ParseValue(value, new HashSet<object>(ReferenceEqualityComparer.Instance));
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
            case sbyte s:
                return new IntValueNode(s);
            case byte b:
                return new IntValueNode(b);
        }

        var type = value.GetType();

        if (type.IsValueType && Converter.TryConvert(
            type, typeof(string), value, out var converted)
            && converted is string c)
        {
            return new StringValueNode(c);
        }

        if (set.Add(value))
        {
            if (value is IReadOnlyDictionary<string, object> dict)
            {
                var fields = new List<ObjectFieldNode>();
                foreach (var field in dict)
                {
                    fields.Add(new ObjectFieldNode(
                        field.Key,
                        ParseValue(field.Value, set)));
                }

                set.Remove(value);

                return new ObjectValueNode(fields);
            }

            if (value is IReadOnlyList<object> list)
            {
                var valueList = new List<IValueNode>();
                foreach (var element in list)
                {
                    valueList.Add(ParseValue(element, set));
                }

                set.Remove(value);

                return new ListValueNode(valueList);
            }

            var valueNode = ParseValue(_objectToDictConverter.Convert(value), set);

            set.Remove(value);

            return valueNode;
        }

        throw new SerializationException(
            TypeResources.AnyType_CycleInObjectGraph,
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
            case string:
            case short:
            case int:
            case long:
            case float:
            case double:
            case decimal:
            case bool:
            case sbyte:
            case byte:
                resultValue = runtimeValue;
                return true;

            default:
                var type = runtimeValue.GetType();

                if (type.IsValueType &&
                    Converter.TryConvert(type, typeof(string), runtimeValue, out var c) &&
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
        object? elementValue;
        runtimeValue = null;
        switch (resultValue)
        {
            case IDictionary<string, object> dictionary:
                {
                    var result = new Dictionary<string, object?>();
                    foreach (var element in dictionary)
                    {
                        if (TryDeserialize(element.Value, out elementValue))
                        {
                            result[element.Key] = elementValue;
                        }
                        else
                        {
                            return false;
                        }
                    }

                    runtimeValue = result;
                    return true;
                }

            case IList list:
                {
                    var result = new object?[list.Count];
                    for (var i = 0; i < list.Count; i++)
                    {
                        if (TryDeserialize(list[i], out elementValue))
                        {
                            result[i] = elementValue;
                        }
                        else
                        {
                            return false;
                        }
                    }

                    runtimeValue = result;
                    return true;
                }

            // TODO: this is only done for a bug in schema stitching and needs to be removed
            // once we have release stitching 2.
            case IValueNode literal:
                runtimeValue = ParseLiteral(literal);
                return true;

            default:
                runtimeValue = resultValue;
                return true;
        }
    }
}
