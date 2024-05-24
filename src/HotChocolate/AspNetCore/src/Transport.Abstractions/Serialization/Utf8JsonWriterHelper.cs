using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using HotChocolate.Language;
using HotChocolate.Transport.Http;

namespace HotChocolate.Transport.Serialization;

/// <summary>
/// Helper methods for writing <see cref="OperationRequest"/> to a <see cref="Utf8JsonWriter"/>.
/// </summary>
internal static class Utf8JsonWriterHelper
{
    public static void WriteOperationRequest(Utf8JsonWriter writer, OperationBatchRequest batchRequest)
    {
        writer.WriteStartArray();

        foreach (var request in batchRequest.Requests)
        {
            switch (request)
            {
                case OperationRequest operationRequest:
                    WriteOperationRequest(writer, operationRequest);
                    break;
                
                case VariableBatchRequest variableBatchRequest:
                    WriteOperationRequest(writer, variableBatchRequest);
                    break;
                
                default:
                    throw new NotSupportedException(
                        "The operation request type is not supported.");
            }
        }
        
        writer.WriteEndArray();
    }
    
    public static void WriteOperationRequest(Utf8JsonWriter writer, OperationRequest request)
    {
        writer.WriteStartObject();

        if (request.Id is not null)
        {
            writer.WriteString(Utf8GraphQLRequestProperties.IdProp, request.Id);
        }

        if (request.Query is not null)
        {
            writer.WriteString(Utf8GraphQLRequestProperties.QueryProp, request.Query);
        }

        if (request.OperationName is not null)
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
    
    public static void WriteOperationRequest(Utf8JsonWriter writer, VariableBatchRequest request)
    {
        writer.WriteStartObject();

        if (request.Id is not null)
        {
            writer.WriteString(Utf8GraphQLRequestProperties.IdProp, request.Id);
        }
 
        if (request.Query is not null)
        {
            writer.WriteString(Utf8GraphQLRequestProperties.QueryProp, request.Query);
        }

        if (request.OperationName is not null)
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
        if (value is null or NullValueNode or FileReference or FileReferenceNode)
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

            case byte[] bytes:
                writer.WriteBase64StringValue(bytes);
                break;
            
            case IReadOnlyList<IReadOnlyDictionary<string, object?>> list:
                WriteList(writer, list);
                break;

            case IList list:
                WriteList(writer, list);
                break;

            case JsonDocument doc:
                doc.RootElement.WriteTo(writer);
                break;

            case JsonElement element:
                element.WriteTo(writer);
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
            writer.WritePropertyName(item.Key);
            WriteFieldValue(writer, item.Value);
        }

        writer.WriteEndObject();
    }
    
    private static void WriteList<T>(
        Utf8JsonWriter writer,
        IReadOnlyList<T> list)
    {
        writer.WriteStartArray();

        for (var i = 0; i < list.Count; i++)
        {
            WriteFieldValue(writer, list[i]);
        }

        writer.WriteEndArray();
    }

    private static void WriteList(
        Utf8JsonWriter writer,
        IList list)
    {
        writer.WriteStartArray();

        for (var i = 0; i < list.Count; i++)
        {
            WriteFieldValue(writer, list[i]);
        }

        writer.WriteEndArray();
    }

    internal static IReadOnlyList<FileReferenceInfo> WriteFilesMap(
        Utf8JsonWriter writer,
        IRequestBody requestBody,
        int? operation = null)
    {
        Dictionary<FileReference, FilePath[]>? files = null;
        CollectFiles(requestBody, ref files);
        
        if (files is null)
        {
            return Array.Empty<FileReferenceInfo>();
        }
                
        var fileInfos = new List<FileReferenceInfo>();
        var index = 0;

        writer.WriteStartObject();

        foreach (var item in files)
        {
            var name = index.ToString();
            fileInfos.Add(new FileReferenceInfo(item.Key, name));

            writer.WritePropertyName(name);
            writer.WriteStartArray();

            foreach (var path in item.Value)
            {
                writer.WriteStringValue(path.ToString(operation));
            }

            writer.WriteEndArray();

            index++;
        }

        writer.WriteEndObject();

        return fileInfos;
    }

    private static void CollectFiles(IRequestBody requestBody, ref Dictionary<FileReference, FilePath[]>? files)
    {
        switch (requestBody)
        {
            case OperationRequest operationRequest:
                if (operationRequest.Variables is not null)
                {
                    CollectFiles(operationRequest.Variables, FilePath.Root, ref files);
                    break;
                }

                if (operationRequest.VariablesNode is not null)
                {
                    CollectFiles(operationRequest.VariablesNode, FilePath.Root, ref files);
                }
                break;

            case VariableBatchRequest variableBatchRequest:
                if (variableBatchRequest.Variables is not null)
                {
                    foreach (var variableSet in variableBatchRequest.Variables)
                    {
                        CollectFiles(variableSet, FilePath.Root, ref files);
                    }
                    break;
                }

                if (variableBatchRequest.VariablesNode is not null)
                {
                    foreach (var variableSet in variableBatchRequest.VariablesNode)
                    {
                        CollectFiles(variableSet, FilePath.Root, ref files);
                    }
                }
                break;

            case OperationBatchRequest batchRequest:
                foreach (var request in batchRequest.Requests)
                {
                    CollectFiles(request, ref files);
                }
                break;
        }
    }

    private static void CollectFiles(
        object? obj,
        FilePath path,
        ref Dictionary<FileReference, FilePath[]>? files)
    {
        if (obj is null)
        {
            return;
        }

        switch (obj)
        {
            case FileReferenceNode fileUpload:
                CollectFile(fileUpload.Value, path, ref files);
                break;

            case FileReference fileUpload:
                CollectFile(fileUpload, path, ref files);
                break;

            case ObjectValueNode objectValue:
                CollectFiles(objectValue, path, ref files);
                break;

            case ListValueNode listValue:
                CollectFiles(listValue, path, ref files);
                break;

            case Dictionary<string, object?> dict:
                CollectFiles(dict, path, ref files);
                break;

            case IList list:
                CollectFiles(list, path, ref files);
                break;
        }
    }

    private static void CollectFiles(
        Dictionary<string, object?> dict,
        FilePath path,
        ref Dictionary<FileReference, FilePath[]>? files)
    {
        foreach (var item in dict)
        {
            if (item.Value is null)
            {
                continue;
            }

            var current = path.Append(item.Key);
            CollectFiles(item.Value, current, ref files);
        }
    }

    private static void CollectFiles(
        IList list,
        FilePath path,
        ref Dictionary<FileReference, FilePath[]>? files)
    {
        for (var i = 0; i < list.Count; i++)
        {
            var item = list[i];

            if (item is null)
            {
                continue;
            }

            var current = path.Append(i);
            CollectFiles(item, current, ref files);
        }
    }

    private static void CollectFiles(
        ObjectValueNode obj,
        FilePath path,
        ref Dictionary<FileReference, FilePath[]>? files)
    {
        foreach (var item in obj.Fields)
        {
            if (item.Value.Kind == SyntaxKind.NullValue)
            {
                continue;
            }

            var current = path.Append(item.Name.Value);
            CollectFiles(item.Value, current, ref files);
        }
    }

    private static void CollectFiles(
        ListValueNode obj,
        FilePath path,
        ref Dictionary<FileReference, FilePath[]>? files)
    {
        for (var i = 0; i < obj.Items.Count; i++)
        {
            var item = obj.Items[i];

            if (item.Kind == SyntaxKind.NullValue)
            {
                continue;
            }

            var current = path.Append(i);
            CollectFiles(item, current, ref files);
        }
    }

    private static void CollectFile(
        FileReference file,
        FilePath path,
        ref Dictionary<FileReference, FilePath[]>? files)
    {
        files ??= new Dictionary<FileReference, FilePath[]>();

        if (files.TryGetValue(file, out var list))
        {
            Array.Resize(ref list, list.Length + 1);
            list[list.Length - 1] = path;
        }
        else
        {
            list = [path,];
            files.Add(file, list);
        }
    }

    private abstract class FilePath(FilePath? parent)
    {
        public FilePath? Parent { get; } = parent;

        public FilePath Append(string name) => new NameFilePath(this, name);

        public FilePath Append(int index) => new IndexFilePath(this, index);

        public static RootFilePath Root { get; } = new();

        public override string ToString()
            => ToString(null);

        public string ToString(int? operation)
        {
            var sb = new StringBuilder();
            var current = this;
            var first = true;

            while (current is not RootFilePath and not null)
            {
                if (current is NameFilePath name)
                {
                    sb.Insert(0, name.Name);

                    if (!first)
                    {
                        sb.Insert(name.Name.Length, '.');
                    }
                }
                else if (current is IndexFilePath index)
                {
                    var indexString = index.Index.ToString();
                    sb.Insert(0, indexString);

                    if (!first)
                    {
                        sb.Insert(indexString.Length, '.');
                    }
                }

                first = false;
                current = current.Parent;
            }

            sb.Insert(0, "variables.");

            if (operation.HasValue)
            {
                var indexString = operation.Value.ToString();
                sb.Insert(0, indexString);
                sb.Insert(indexString.Length, '.');
            }

            return sb.ToString();
        }
    }

    private sealed class RootFilePath : FilePath
    {
        public RootFilePath() : base(null) { }
    }

    private sealed class NameFilePath : FilePath
    {
        public NameFilePath(FilePath? parent, string name) : base(parent)
        {
            Name = name;
        }

        public string Name { get; }
    }

    private sealed class IndexFilePath : FilePath
    {
        public IndexFilePath(FilePath? parent, int index) : base(parent)
        {
            Index = index;
        }

        public int Index { get; }
    }
}
