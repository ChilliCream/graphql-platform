using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Types.MongoDb.Resources;
using HotChocolate.Utilities;
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
    public BsonType()
        : this(
            MongoDbScalarNames.Bson,
            MongoDbTypesResources.Bson_Type_Description,
            BindingBehavior.Implicit)
    {
    }

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

    /// <inheritdoc />
    public override Type RuntimeType => typeof(BsonValue);

    /// <inheritdoc />
    public override bool IsInstanceOfType(IValueNode valueSyntax)
    {
        if (valueSyntax is null)
        {
            throw new ArgumentNullException(nameof(valueSyntax));
        }

        switch (valueSyntax)
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

    private BsonValue? ParseLiteralToBson(IValueNode literal)
    {
        switch (literal)
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
                BsonValue?[] values = new BsonValue[lvn.Items.Count];
                for (var i = 0; i < lvn.Items.Count; i++)
                {
                    values[i] = ParseLiteralToBson(lvn.Items[i]);
                }

                return new BsonArray(values);

            case ObjectValueNode ovn:
                BsonDocument document = new();
                foreach (var field in ovn.Fields)
                {
                    document.Add(field.Name.Value, ParseLiteralToBson(field.Value));
                }

                return document;

            case NullValueNode:
                return BsonNull.Value;

            default:
                throw ThrowHelper.Bson_CouldNotParseLiteral(this, literal);
        }
    }

    /// <inheritdoc />
    public override object? ParseLiteral(IValueNode valueSyntax)
    {
        return ParseLiteralToBson(valueSyntax);
    }

    /// <inheritdoc />
    public override IValueNode ParseValue(object? runtimeValue)
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
                fields.Add(new ObjectFieldNode(field.Name, ParseValue(field.Value)));
            }

            return new ObjectValueNode(fields);
        }

        if (value is BsonArray arr)
        {
            List<IValueNode> valueList = [];
            foreach (var element in arr)
            {
                valueList.Add(ParseValue(element));
            }

            return new ListValueNode(valueList);
        }

        var mappedValue = BsonTypeMapper.MapToDotNetValue(value);
        var type = mappedValue.GetType();

        if (type.IsValueType &&
            Converter.TryConvert(type, typeof(string), mappedValue, out var converted) &&
            converted is string c)
        {
            return new StringValueNode(c);
        }

        throw ThrowHelper.Bson_CouldNotParseValue(this, runtimeValue);
    }

    /// <inheritdoc />
    public override IValueNode ParseResult(object? resultValue) =>
        ParseValue(resultValue);

    public override bool TrySerialize(object? runtimeValue, out object? resultValue)
    {
        resultValue = null;
        if (runtimeValue is null or BsonNull)
        {
            return true;
        }

        switch (runtimeValue)
        {
            case BsonArray arr:
                var res = new object?[arr.Count];
                for (var i = 0; i < arr.Count; i++)
                {
                    if (!TrySerialize(arr[i], out var s))
                    {
                        return false;
                    }

                    res[i] = s;
                }

                resultValue = res;
                return true;

            case BsonDocument doc:
                Dictionary<string, object?> docRes = new();
                foreach (var element in doc)
                {
                    if (!TrySerialize(element.Value, out var s))
                    {
                        return false;
                    }

                    docRes[element.Name] = s;
                }

                resultValue = docRes;
                return true;

            case BsonDateTime dateTime:
                var parsedDateTime = dateTime.ToNullableUniversalTime();
                if (Converter.TryConvert(parsedDateTime, out string? formattedDateTime))
                {
                    resultValue = formattedDateTime;
                    return true;
                }

                return false;

            case BsonTimestamp timeStamp:
                resultValue = timeStamp.Value;
                return true;

            case BsonObjectId objectId:
                resultValue = objectId.Value.ToString();
                return true;

            case BsonString s:
                resultValue = s.Value;
                return true;

            case BsonInt32 i:
                resultValue = i.Value;
                return true;

            case BsonInt64 l:
                resultValue = l.Value;
                return true;

            case BsonDouble f:
                resultValue = f.Value;
                return true;

            case BsonBinaryData bd:
                resultValue = Convert.ToBase64String(bd.Bytes);
                return true;

            // The range of Decimal128 is different. Therefor we have to serialize
            // it as a string, or else information loss could occur
            // see https://jira.mongodb.org/browse/CSHARP-2210
            case BsonDecimal128 d:
                resultValue = d.Value.ToString();
                return true;

            case BsonBoolean b:
                resultValue = b.Value;
                return true;

            case BsonValue a:
                var dotNetValue = BsonTypeMapper.MapToDotNetValue(a);

                var type = dotNetValue.GetType();

                if (type.IsValueType &&
                    Converter.TryConvert(type, typeof(string), dotNetValue, out var c) &&
                    c is string casted)
                {
                    resultValue = casted;
                    return true;
                }

                resultValue = null;
                return false;

            case IValueNode literal:
                resultValue = ParseLiteral(literal);
                return true;

            default:
                resultValue = null;
                return false;
        }
    }

    /// <inheritdoc />
    public override bool TryDeserialize(object? resultValue, out object? runtimeValue)
    {
        object? elementValue;
        runtimeValue = null;
        switch (resultValue)
        {
            case IDictionary<string, object> dictionary:
                {
                    var result = new BsonDocument();
                    foreach (var element in dictionary)
                    {
                        if (TryDeserialize(element.Value, out elementValue))
                        {
                            result[element.Key] = (BsonValue?)elementValue;
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
                    var result = new BsonValue?[list.Count];
                    for (var i = 0; i < list.Count; i++)
                    {
                        if (TryDeserialize(list[i], out elementValue))
                        {
                            result[i] = (BsonValue?)elementValue;
                        }
                        else
                        {
                            return false;
                        }
                    }

                    runtimeValue = new BsonArray(result);
                    return true;
                }

            case IValueNode literal:
                runtimeValue = ParseLiteral(literal);
                return true;

            default:
                runtimeValue = BsonTypeMapper.MapToBsonValue(resultValue);
                return true;
        }
    }
}
