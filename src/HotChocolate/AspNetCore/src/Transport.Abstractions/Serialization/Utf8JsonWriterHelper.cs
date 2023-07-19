using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.Json;
using HotChocolate.Language;

namespace HotChocolate.Transport.Serialization;

/// <summary>
/// Helper methods for writing <see cref="OperationRequest"/> to a <see cref="Utf8JsonWriter"/>.
/// </summary>
internal static class Utf8JsonWriterHelper
{
    public static void WriteOperationRequest(Utf8JsonWriter writer, OperationRequest request)
    {
        writer.WriteStartObject();

        if(request.Id is not null)
        {
            writer.WriteString(Utf8GraphQLRequestProperties.IdProp, request.Id);
        }

        if(request.Query is not null)
        {
            writer.WriteString(Utf8GraphQLRequestProperties.QueryProp, request.Query);
        }

        if(request.OperationName is not null)
        {
            writer.WriteString(Utf8GraphQLRequestProperties.OperationNameProp, request.OperationName);
        }

        if (request.ExtensionsNode is not null)
        {
            writer.WritePropertyName(Utf8GraphQLRequestProperties.ExtensionsProp);
            WriteFieldValue(writer, request.ExtensionsNode);
        }
        else if (request.Extensions is not null)
        {
            writer.WritePropertyName(Utf8GraphQLRequestProperties.ExtensionsProp);
            WriteFieldValue(writer, request.Extensions);
        }

        if (request.VariablesNode is not null)
        {
            writer.WritePropertyName(Utf8GraphQLRequestProperties.VariablesProp);
            WriteFieldValue(writer, request.VariablesNode);
        }
        else if (request.Variables is not null)
        {
            writer.WritePropertyName(Utf8GraphQLRequestProperties.VariablesProp);
            WriteFieldValue(writer, request.Variables);
        }

        writer.WriteEndObject();
    }

    internal static void WriteFieldValue(
        Utf8JsonWriter writer,
        object? value)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        switch (value)
        {
            case NullValueNode nullValueNode:
              WriteFieldValue(writer, nullValueNode.Value);
              break;

            case ObjectValueNode objectValue:
                writer.WriteStartObject();

                foreach (var field in objectValue.Fields)
                {
                    writer.WritePropertyName(field.Name.Value);
                    WriteFieldValue(writer, field.Value);
                }

                writer.WriteEndObject();
                break;

            case ListValueNode listValue:
                writer.WriteStartArray();

                foreach (var item in listValue.Items)
                {
                    WriteFieldValue(writer, item);
                }

                writer.WriteEndArray();
                break;

            case StringValueNode stringValue:
                writer.WriteStringValue(stringValue.Value);
                break;

            case IntValueNode intValue:
                writer.WriteRawValue(intValue.Value);
                break;

            case FloatValueNode floatValue:
                writer.WriteRawValue(floatValue.Value);
                break;

            case BooleanValueNode booleanValue:
                writer.WriteBooleanValue(booleanValue.Value);
                break;

            case EnumValueNode enumValue:
                writer.WriteStringValue(enumValue.Value);
                break;

            case Dictionary<string, object?> dict:
                WriteDictionary(writer, dict);
                break;

            case IList list:
                WriteList(writer, list);
                break;

            case string s:
                writer.WriteStringValue(s);
                break;

            case byte b:
                writer.WriteNumberValue(b);
                break;

            case short s:
                writer.WriteNumberValue(s);
                break;

            case ushort s:
                writer.WriteNumberValue(s);
                break;

            case int i:
                writer.WriteNumberValue(i);
                break;

            case uint i:
                writer.WriteNumberValue(i);
                break;

            case long l:
                writer.WriteNumberValue(l);
                break;

            case ulong l:
                writer.WriteNumberValue(l);
                break;

            case float f:
                writer.WriteNumberValue(f);
                break;

            case double d:
                writer.WriteNumberValue(d);
                break;

            case decimal d:
                writer.WriteNumberValue(d);
                break;

            case bool b:
                writer.WriteBooleanValue(b);
                break;

            case Uri u:
                writer.WriteStringValue(u.ToString());
                break;

            default:
                writer.WriteStringValue(value.ToString());
                break;
        }
    }

    private static void WriteDictionary(
        Utf8JsonWriter writer,
        Dictionary<string, object?> dict)
    {
        writer.WriteStartObject();

        foreach (var item in dict)
        {
            if (item.Value is null)
            {
                continue;
            }

            writer.WritePropertyName(item.Key);
            WriteFieldValue(writer, item.Value);
        }

        writer.WriteEndObject();
    }

    private static void WriteList(
        Utf8JsonWriter writer,
        IList list)
    {
        writer.WriteStartArray();

        for (var i = 0; i < list.Count; i++)
        {
            var element = list[i];

            if (element is null)
            {
                continue;
            }

            WriteFieldValue(writer, element);
        }

        writer.WriteEndArray();
    }
}
