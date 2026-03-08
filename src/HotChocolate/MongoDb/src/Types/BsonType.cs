using System.Globalization;
using System.Text.Json;
using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Text.Json;
using HotChocolate.Types.MongoDb.Resources;
using HotChocolate.Utilities;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;

namespace HotChocolate.Types.MongoDb;

/// <summary>
/// BSON is a binary format in which zero or more ordered key/value pairs are stored as a single
/// entity.
/// The results are returned as JSON objects
/// </summary>
public class BsonType : ScalarType
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BsonType"/> class.
    /// </summary>
    public BsonType(
        string name,
        string? description = null,
        BindingBehavior bind = BindingBehavior.Explicit)
        : base(name, bind)
    {
        SpecifiedBy = new Uri("https://bsonspec.org/spec.html");
        Description = description;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BsonType"/> class.
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public BsonType()
        : this(
            MongoDbScalarNames.Bson,
            MongoDbTypesResources.Bson_Type_Description,
            BindingBehavior.Implicit)
    {
    }

    /// <inheritdoc />
    public override Type RuntimeType => typeof(BsonValue);

    /// <inheritdoc />
    public override ScalarSerializationType SerializationType => ScalarSerializationType.Any;

    /// <inheritdoc />
    public override object CoerceInputLiteral(IValueNode valueLiteral)
    {
        switch (valueLiteral)
        {
            case StringValueNode svn:
                return new BsonString(svn.Value);

            case IntValueNode ivn:
                return new BsonInt64(long.Parse(ivn.Value, CultureInfo.InvariantCulture));

            case FloatValueNode fvn
                when double.TryParse(fvn.Value,
                    NumberStyles.Float | NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out var f):
                return new BsonDouble(f);

            case FloatValueNode fvn:
                return new BsonDecimal128(
                    decimal.Parse(fvn.Value, CultureInfo.InvariantCulture));

            case BooleanValueNode bvn:
                return new BsonBoolean(bvn.Value);

            case ListValueNode lvn:
                var values = new BsonValue[lvn.Items.Count];
                for (var i = 0; i < lvn.Items.Count; i++)
                {
                    values[i] = (BsonValue)CoerceInputLiteral(lvn.Items[i]);
                }

                return new BsonArray(values);

            case ObjectValueNode ovn:
                BsonDocument document = [];
                foreach (var field in ovn.Fields)
                {
                    document.Add(field.Name.Value, (BsonValue)CoerceInputLiteral(field.Value));
                }

                return document;

            case NullValueNode:
                return BsonNull.Value;

            default:
                throw ThrowHelper.Bson_CouldNotParseLiteral(this, valueLiteral);
        }
    }

    public override object CoerceInputValue(JsonElement inputValue, IFeatureProvider context)
    {
        switch (inputValue.ValueKind)
        {
            case JsonValueKind.String:
                return new BsonString(inputValue.GetString()!);

            case JsonValueKind.Number:
                if (inputValue.TryGetInt64(out var longValue))
                {
                    return new BsonInt64(longValue);
                }
                if (inputValue.TryGetDouble(out var doubleValue))
                {
                    return new BsonDouble(doubleValue);
                }
                return new BsonDecimal128(inputValue.GetDecimal());

            case JsonValueKind.True:
                return BsonBoolean.True;

            case JsonValueKind.False:
                return BsonBoolean.False;

            case JsonValueKind.Array:
                var arrayLength = inputValue.GetArrayLength();
                var values = new BsonValue[arrayLength];
                var index = 0;
                foreach (var element in inputValue.EnumerateArray())
                {
                    values[index++] = (BsonValue)CoerceInputValue(element, context);
                }
                return new BsonArray(values);

            case JsonValueKind.Object:
                var document = new BsonDocument();
                foreach (var property in inputValue.EnumerateObject())
                {
                    document.Add(property.Name, (BsonValue)CoerceInputValue(property.Value, context));
                }
                return document;

            case JsonValueKind.Null:
            case JsonValueKind.Undefined:
                return BsonNull.Value;

            default:
                throw ThrowHelper.Bson_CouldNotParseValue(this, inputValue);
        }
    }

    public override void CoerceOutputValue(object? runtimeValue, ResultElement resultValue)
    {
        if (runtimeValue is null or BsonNull)
        {
            resultValue.SetNullValue();
            return;
        }

        switch (runtimeValue)
        {
            case BsonString s:
                resultValue.SetStringValue(s.Value);
                break;

            case BsonInt32 i:
                resultValue.SetNumberValue(i.Value);
                break;

            case BsonInt64 l:
                resultValue.SetNumberValue(l.Value);
                break;

            case BsonDouble d:
                resultValue.SetNumberValue(d.Value);
                break;

            case BsonDecimal128 dec:
                // The range of Decimal128 is different. Therefore, we have to serialize
                // it as a string, or else information loss could occur
                // see https://jira.mongodb.org/browse/CSHARP-2210
                resultValue.SetStringValue(dec.Value.ToString());
                break;

            case BsonBoolean b:
                resultValue.SetBooleanValue(b.Value);
                break;

            case BsonObjectId objectId:
                resultValue.SetStringValue(objectId.Value.ToString());
                break;

            case BsonDateTime dateTime:
                var parsedDateTime = dateTime.ToNullableUniversalTime();
                if (Converter.TryConvert(parsedDateTime, out string? formattedDateTime))
                {
                    resultValue.SetStringValue(formattedDateTime);
                }
                else
                {
                    throw ThrowHelper.Bson_CouldNotParseValue(this, runtimeValue);
                }
                break;

            case BsonTimestamp timeStamp:
                resultValue.SetNumberValue(timeStamp.Value);
                break;

            case BsonBinaryData bd:
                resultValue.SetStringValue(Convert.ToBase64String(bd.Bytes));
                break;

            case BsonArray arr:
                resultValue.SetArrayValue(arr.Count);
                using (var enumerator = arr.GetEnumerator())
                {
                    foreach (var element in resultValue.EnumerateArray())
                    {
                        enumerator.MoveNext();
                        CoerceOutputValue(enumerator.Current, element);
                    }
                }
                break;

            case BsonDocument doc:
                resultValue.SetObjectValue(doc.ElementCount);
                using (var enumerator = doc.GetEnumerator())
                {
                    foreach (var property in resultValue.EnumerateObject())
                    {
                        enumerator.MoveNext();
                        property.Value.SetPropertyName(enumerator.Current.Name);
                        CoerceOutputValue(enumerator.Current.Value, property.Value);
                    }
                }
                break;

            case BsonValue a:
                var dotNetValue = BsonTypeMapper.MapToDotNetValue(a);
                var type = dotNetValue.GetType();

                if (type.IsValueType
                    && Converter.TryConvert(type, typeof(string), dotNetValue, out var c, out _)
                    && c is string casted)
                {
                    resultValue.SetStringValue(casted);
                }
                else
                {
                    throw ThrowHelper.Bson_CouldNotParseValue(this, runtimeValue);
                }
                break;

            default:
                throw ThrowHelper.Bson_CouldNotParseValue(this, runtimeValue);
        }
    }

    public override IValueNode ValueToLiteral(object? runtimeValue)
    {
        if (runtimeValue is null)
        {
            return NullValueNode.Default;
        }

        if (runtimeValue is not BsonValue value)
        {
            value = BsonTypeMapper.MapToBsonValue(runtimeValue);
        }

        switch (value)
        {
            case BsonString s:
                return new StringValueNode(s.Value);

            case BsonInt32 i:
                return new IntValueNode(i.Value);

            case BsonInt64 l:
                return new IntValueNode(l.Value);

            case BsonDouble f:
                return new FloatValueNode(f.Value);

            // The range of Decimal128 is different. Therefor we have to serialize
            // it as a string, or else information loss could occur
            // see https://jira.mongodb.org/browse/CSHARP-2210
            case BsonDecimal128 d:
                return new StringValueNode(d.Value.ToString());

            case BsonBoolean b:
                return new BooleanValueNode(b.Value);

            case BsonObjectId s:
                return new StringValueNode(s.Value.ToString());

            case BsonDateTime dateTime when Converter
                .TryConvert(dateTime.ToNullableUniversalTime(), out string formattedDateTime):
                return new StringValueNode(formattedDateTime);

            case BsonBinaryData bd:
                return new StringValueNode(Convert.ToBase64String(bd.Bytes));

            case BsonTimestamp timeStamp:
                return new IntValueNode(timeStamp.Value);
        }

        if (value is BsonDocument doc)
        {
            List<ObjectFieldNode> fields = [];
            foreach (var field in doc)
            {
                fields.Add(new ObjectFieldNode(field.Name, ValueToLiteral(field.Value)));
            }

            return new ObjectValueNode(fields);
        }

        if (value is BsonArray arr)
        {
            List<IValueNode> valueList = [];
            foreach (var element in arr)
            {
                valueList.Add(ValueToLiteral(element));
            }

            return new ListValueNode(valueList);
        }

        var mappedValue = BsonTypeMapper.MapToDotNetValue(value);
        var type = mappedValue.GetType();

        if (type.IsValueType
            && Converter.TryConvert(type, typeof(string), mappedValue, out var converted, out _)
            && converted is string c)
        {
            return new StringValueNode(c);
        }

        throw ThrowHelper.Bson_CouldNotParseValue(this, runtimeValue);
    }
}
