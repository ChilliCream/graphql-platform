using System.Collections;
using System.Globalization;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Text.Json;
using HotChocolate.Types.Descriptors.Configurations;
using HotChocolate.Utilities;

namespace HotChocolate.Types;

public class AnyType : ScalarType
{
    private readonly ObjectValueToDictionaryConverter _objectValueToDictConverter = new();
    private ObjectToDictionaryConverter _objectToDictConverter = null!;

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
        SerializationType = ScalarSerializationType.Any;
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
        ScalarTypeConfiguration configuration)
    {
        base.OnCompleteType(context, configuration);
        _objectToDictConverter = new ObjectToDictionaryConverter(Converter);
    }

    public override bool IsValueCompatible(IValueNode literal)
    {
        ArgumentNullException.ThrowIfNull(literal);

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

    public override object? CoerceInputLiteral(IValueNode literal)
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
                throw new LeafCoercionException(
                    TypeResourceHelper.Scalar_Cannot_CoerceInputLiteral(Name, literal.GetType()),
                    this);
        }
    }

    public override IValueNode CoerceInputValue(object? value)
    {
        return value is null
            ? NullValueNode.Default
            : ParseValue(value, new HashSet<object>(ReferenceEqualityComparer.Instance));
    }

    private IValueNode ParseValue(object? value, HashSet<object> set)
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
            type, typeof(string), value, out var converted, out _)
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

        throw new LeafCoercionException(
            TypeResources.AnyType_CycleInObjectGraph,
            this);
    }

    public override bool TryCoerceOutputValue(object? runtimeValue, ResultElement resultValue)
    {
        if (runtimeValue is null)
        {
            resultValue.SetNullValue();
            return true;
        }

        switch (runtimeValue)
        {
            case string castedString:
                resultValue.SetStringValue(castedString);
                return true;

            case short castedShort:
                resultValue.SetNumberValue(castedShort);
                return true;

            case int castedInt:
                resultValue.SetNumberValue(castedInt);
                return true;

            case long castedLong:
                resultValue.SetNumberValue(castedLong);
                return true;

            case float castedFloat:
                resultValue.SetNumberValue(castedFloat);
                return true;

            case double castedDouble:
                resultValue.SetNumberValue(castedDouble);
                return true;

            case decimal castedDecimal:
                resultValue.SetNumberValue(castedDecimal);
                return true;

            case bool castedBool:
                resultValue.SetBooleanValue(castedBool);
                return true;

            case sbyte castedSByte:
                resultValue.SetNumberValue(castedSByte);
                return true;

            case byte castedByte:
                resultValue.SetNumberValue(castedByte);
                return true;

            case ushort castedUShort:
                resultValue.SetNumberValue(castedUShort);
                return true;

            case uint castedUInt:
                resultValue.SetNumberValue(castedUInt);
                return true;

            case ulong castedULong:
                resultValue.SetNumberValue(castedULong);
                return true;

            default:
                var type = runtimeValue.GetType();

                if (type.IsValueType
                    && Converter.TryConvert(type, typeof(string), runtimeValue, out var c, out _)
                    && c is string casted)
                {
                    resultValue = casted;
                    return true;
                }

                resultValue = _objectToDictConverter.Convert(runtimeValue);
                return true;
        }
    }
}
