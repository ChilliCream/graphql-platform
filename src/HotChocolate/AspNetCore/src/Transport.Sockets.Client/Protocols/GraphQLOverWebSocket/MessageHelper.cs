using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Language;
using HotChocolate.Transport.Sockets.Client.Helpers;
using static System.Net.WebSockets.WebSocketMessageType;
using static HotChocolate.Transport.Sockets.Client.Protocols.GraphQLOverWebSocket.Utf8MessageProperties;

namespace HotChocolate.Transport.Sockets.Client.Protocols.GraphQLOverWebSocket;

internal static class MessageHelper
{
    public static async ValueTask SendConnectionInitMessage<T>(
        this WebSocket socket,
        T payload,
        CancellationToken ct)
    {
        using var arrayWriter = new ArrayWriter();
        await using var jsonWriter = new Utf8JsonWriter(arrayWriter, JsonDefaults.WriterOptions);
        jsonWriter.WriteStartObject();
        jsonWriter.WriteString(TypeProp, Utf8Messages.ConnectionInitialize);

        if (payload is not null)
        {
            jsonWriter.WritePropertyName(PayloadProp);
            JsonSerializer.Serialize(jsonWriter, payload, JsonDefaults.SerializerOptions);
        }

        jsonWriter.WriteEndObject();
        await jsonWriter.FlushAsync(ct).ConfigureAwait(false);

#if NET5_0_OR_GREATER
        await socket.SendAsync(arrayWriter.Body, Text, true, ct).ConfigureAwait(false);
#else
        await socket.SendAsync(arrayWriter.ToArraySegment(), Text, true, ct).ConfigureAwait(false);
#endif
    }

    public static async ValueTask SendSubscribeMessageAsync(
        this WebSocket socket,
        string operationSessionId,
        OperationRequest request,
        CancellationToken ct)
    {
        using var arrayWriter = new ArrayWriter();
        await using var jsonWriter = new Utf8JsonWriter(arrayWriter, JsonDefaults.WriterOptions);

        jsonWriter.WriteStartObject();
        jsonWriter.WriteString(IdProp, operationSessionId);
        jsonWriter.WriteString(TypeProp, Utf8Messages.Subscribe);
        jsonWriter.WritePropertyName(PayloadProp);

        jsonWriter.WriteStartObject();

        if(request.Id is not null)
        {
            jsonWriter.WriteString(IdProp, request.Id);
        }

        if(request.Query is not null)
        {
            jsonWriter.WriteString(QueryProp, request.Query);
        }

        if(request.OperationName is not null)
        {
            jsonWriter.WriteString(OperationNameProp, request.OperationName);
        }

        if (request.ExtensionsNode is not null)
        {
            jsonWriter.WritePropertyName(ExtensionsProp);
            WriteFieldValue(jsonWriter, request.ExtensionsNode);
        }
        else if (request.Extensions is not null)
        {
            jsonWriter.WritePropertyName(ExtensionsProp);
            WriteFieldValue(jsonWriter, request.Extensions);
        }

        if (request.Variables is not null)
        {
            jsonWriter.WritePropertyName(VariablesProp);
            WriteFieldValue(jsonWriter, request.VariablesNode);
        }
        else if (request.Variables is not null)
        {
            jsonWriter.WritePropertyName(VariablesProp);
            WriteFieldValue(jsonWriter, request.Variables);
        }

        jsonWriter.WriteEndObject();


        jsonWriter.WriteEndObject();
        await jsonWriter.FlushAsync(ct).ConfigureAwait(false);

#if NET5_0_OR_GREATER
        await socket.SendAsync(arrayWriter.Body, Text, true, ct).ConfigureAwait(false);
#else
        await socket.SendAsync(arrayWriter.ToArraySegment(), Text, true, ct).ConfigureAwait(false);
#endif
    }



    public static async ValueTask SendCompleteMessageAsync(
        this WebSocket socket,
        string operationSessionId,
        CancellationToken ct)
    {
        using var arrayWriter = new ArrayWriter();
        await using var jsonWriter = new Utf8JsonWriter(arrayWriter, JsonDefaults.WriterOptions);
        jsonWriter.WriteStartObject();
        jsonWriter.WriteString(IdProp, operationSessionId);
        jsonWriter.WriteString(TypeProp, Utf8Messages.Complete);
        jsonWriter.WriteEndObject();
        await jsonWriter.FlushAsync(ct).ConfigureAwait(false);

#if NET5_0_OR_GREATER
        await socket.SendAsync(arrayWriter.Body, Text, true, ct).ConfigureAwait(false);
#else
        await socket.SendAsync(arrayWriter.ToArraySegment(), Text, true, ct).ConfigureAwait(false);
#endif
    }

    public static async ValueTask SendPongMessageAsync(
        this WebSocket socket,
        CancellationToken ct)
    {
        using var arrayWriter = new ArrayWriter();
        await using var jsonWriter = new Utf8JsonWriter(arrayWriter, JsonDefaults.WriterOptions);
        jsonWriter.WriteStartObject();
        jsonWriter.WriteString(TypeProp, Utf8Messages.Pong);
        jsonWriter.WriteEndObject();
        await jsonWriter.FlushAsync(ct).ConfigureAwait(false);

#if NET5_0_OR_GREATER
        await socket.SendAsync(arrayWriter.Body, Text, true, ct).ConfigureAwait(false);
#else
        await socket.SendAsync(arrayWriter.ToArraySegment(), Text, true, ct).ConfigureAwait(false);
#endif
    }

    private static void WriteFieldValue(
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
