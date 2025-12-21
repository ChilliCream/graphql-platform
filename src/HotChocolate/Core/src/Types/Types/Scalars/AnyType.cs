using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.Json;
using HotChocolate.Language;
using HotChocolate.Text.Json;
using static HotChocolate.Utilities.ThrowHelper;

namespace HotChocolate.Types;

public class AnyType : ScalarType
{
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

    public override ScalarSerializationType SerializationType => ScalarSerializationType.Any;

    public override bool IsValueCompatible(IValueNode literal)
    {
        switch (literal)
        {
            case StringValueNode:
            case IntValueNode:
            case FloatValueNode:
            case BooleanValueNode:
            case ListValueNode:
            case ObjectValueNode:
                return true;

            default:
                return false;
        }
    }

    public override object CoerceInputLiteral(IValueNode literal)
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
                var list = new List<object?>();
                foreach (var item in lvn.Items)
                {
                    list.Add(CoerceInputLiteral(item));
                }
                return list;

            case ObjectValueNode ovn:
                var obj = new Dictionary<string, object?>();
                foreach (var field in ovn.Fields)
                {
                    obj[field.Name.Value] = CoerceInputLiteral(field.Value);
                }
                return obj;

            case NullValueNode:
                return null!;

            default:
                throw Scalar_Cannot_CoerceInputLiteral(this, literal);
        }
    }

    public override object CoerceInputValue(JsonElement inputValue)
    {
        switch (inputValue.ValueKind)
        {
            case JsonValueKind.String:
                return inputValue.GetString()!;

            case JsonValueKind.Number:
                var rawBytes = JsonMarshal.GetRawUtf8Value(inputValue);

                // Check for decimal point (0x2E) or exponent (0x65 'e', 0x45 'E')
                if (rawBytes.IndexOfAny((byte)'.', (byte)'e', (byte)'E') >= 0)
                {
                    return inputValue.GetDecimal();
                }

                if (inputValue.TryGetInt64(out var longValue))
                {
                    return longValue;
                }

                // Fallback for numbers outside int64 range
                return inputValue.GetDecimal();

            case JsonValueKind.True:
                return true;

            case JsonValueKind.False:
                return false;

            case JsonValueKind.Null:
                return null!;

            case JsonValueKind.Array:
                var list = new List<object?>();
                foreach (var item in inputValue.EnumerateArray())
                {
                    list.Add(CoerceInputValue(item));
                }
                return list;

            case JsonValueKind.Object:
                var obj = new Dictionary<string, object?>();
                foreach (var property in inputValue.EnumerateObject())
                {
                    obj[property.Name] = CoerceInputValue(property.Value);
                }
                return obj;

            default:
                throw Scalar_Cannot_CoerceInputValue(this, inputValue);
        }
    }

    public override void CoerceOutputValue(object runtimeValue, ResultElement resultValue)
    {
        HashSet<object>? processed = null;
        TryCoerceOutputValue(runtimeValue, resultValue, ref processed);
    }

    private static bool TryCoerceOutputValue(object runtimeValue, ResultElement resultValue, ref HashSet<object>? set)
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

            case List<object?> castedList:
            {
                set ??= new HashSet<object>(ReferenceEqualityComparer.Instance);

                try
                {
                    if (!set.Add(castedList))
                    {
                        return false;
                    }

                    using var enumerator = castedList.GetEnumerator();

                    resultValue.SetArrayValue(castedList.Count);
                    foreach (var element in resultValue.EnumerateArray())
                    {
                        enumerator.MoveNext();
                        if (!TryCoerceOutputValue(enumerator.Current!, element, ref set))
                        {
                            element.Invalidate();
                        }
                    }
                    return true;
                }
                finally
                {
                    set?.Remove(castedList);
                }
            }

            case Dictionary<string, object?> castedObject:
            {
                set ??= new HashSet<object>(ReferenceEqualityComparer.Instance);

                try
                {
                    if (!set.Add(castedObject))
                    {
                        return false;
                    }

                    using var enumerator = castedObject.GetEnumerator();

                    resultValue.SetObjectValue(castedObject.Count);
                    foreach (var property in resultValue.EnumerateObject())
                    {
                        enumerator.MoveNext();
                        property.Value.SetPropertyName(enumerator.Current.Key);
                        if (!TryCoerceOutputValue(enumerator.Current.Value!, property.Value, ref set))
                        {
                            property.Value.Invalidate();
                        }
                    }
                    return true;
                }
                finally
                {
                    set?.Remove(castedObject);
                }
            }

            default:
                return false;
        }
    }

    public override IValueNode ValueToLiteral(object runtimeValue)
    {
        return runtimeValue is null
            ? NullValueNode.Default
            : ValueToLiteral(runtimeValue, null, this);
    }

    private static IValueNode ValueToLiteral(object? value, HashSet<object>? set, AnyType type)
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
            case byte b:
                return new IntValueNode(b);
            case sbyte s:
                return new IntValueNode(s);
            case ushort u:
                return new IntValueNode(u);
            case uint u:
                return new IntValueNode(u);
            case ulong u:
                return new IntValueNode(u);

            case float f:
                return new FloatValueNode(f);
            case double d:
                return new FloatValueNode(d);
            case decimal d:
                return new FloatValueNode(d);

            case bool b:
                return new BooleanValueNode(b);
        }

        // Handle collections with cycle detection
        set ??= new HashSet<object>(ReferenceEqualityComparer.Instance);

        if (!set.Add(value))
        {
            throw Scalar_Cannot_ConvertValueToLiteral(type, value);
        }

        try
        {
            switch (value)
            {
                case IReadOnlyList<object?> list:
                    var items = new List<IValueNode>(list.Count);
                    foreach (var item in list)
                    {
                        items.Add(ValueToLiteral(item, set, type));
                    }
                    return new ListValueNode(items);

                case IReadOnlyDictionary<string, object?> obj:
                    var fields = new List<ObjectFieldNode>(obj.Count);
                    foreach (var kvp in obj)
                    {
                        fields.Add(new ObjectFieldNode(kvp.Key, ValueToLiteral(kvp.Value, set, type)));
                    }
                    return new ObjectValueNode(fields);

                default:
                    throw Scalar_Cannot_ConvertValueToLiteral(type, value);
            }
        }
        finally
        {
            set.Remove(value);
        }
    }
}
