using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using StrawberryShake.Json;

namespace StrawberryShake;

/// <summary>
/// The operation result builder will use the transport response and build from it
/// the operation result.
/// </summary>
/// <typeparam name="TResultData">
/// The runtime result.
/// </typeparam>
public abstract class OperationResultBuilder<TResultData>
    : IOperationResultBuilder<JsonDocument, TResultData>
    where TResultData : class
{
    private const string _path = "path";
    private const string _data = "data";
    private const string _errors = "errors";

    protected abstract IOperationResultDataFactory<TResultData> ResultDataFactory { get; }

    public IOperationResult<TResultData> Build(
        Response<JsonDocument> response)
    {
        TResultData? data = null;
        IOperationResultDataInfo? dataInfo = null;
        IReadOnlyList<IClientError>? errors = null;
        Exception? exception = null;

        try
        {
            if (response.Body is { } body)
            {
                if (body.RootElement.TryGetProperty(_data, out JsonElement dataProp) &&
                    dataProp.ValueKind is JsonValueKind.Object)
                {
                    dataInfo = BuildData(dataProp);
                    data = ResultDataFactory.Create(dataInfo);
                }

                if (body.RootElement.TryGetProperty(_errors, out JsonElement errorsProp) &&
                    errorsProp.ValueKind is JsonValueKind.Array)
                {
                    errors = JsonErrorParser.ParseErrors(errorsProp);
                }
            }
        }
        catch (Exception ex)
        {
            exception = ex;
        }

        return new OperationResult<TResultData>(
            data,
            dataInfo,
            ResultDataFactory,
            errors,
            response.Extensions,
            response.ContextData);
    }

    protected abstract IOperationResultDataInfo BuildData(JsonElement dataProp);

    public IOperationResult<TResultData> Patch(
        Response<JsonDocument> response,
        IOperationResult<TResultData> result)
    {
        TResultData? data = null;
        IOperationResultDataInfo? dataInfo = null;
        IReadOnlyList<IClientError>? errors = null;
        Exception? exception = null;

        try
        {
            if (response.Body is { } body)
            {
                if (result.DataInfo is not null &&
                    body.RootElement.TryGetProperty(_path, out JsonElement pathProp) &&
                    body.RootElement.TryGetProperty(_data, out JsonElement dataProp) &&
                    pathProp.ValueKind is JsonValueKind.Array &&
                    dataProp.ValueKind is JsonValueKind.Object)
                {
                    var context = new PatchContext(
                        CreatePath(pathProp),
                        dataProp,
                        result.DataInfo.EntityIds.ToHashSet(),
                        result.DataInfo.PathToEntityIds.ToDictionary(t => t.Key, t => t.Value));

                    dataInfo = PatchData(context);
                    data = ResultDataFactory.Create(dataInfo);
                }

                if (body.RootElement.TryGetProperty(_errors, out JsonElement errorsProp) &&
                    errorsProp.ValueKind is JsonValueKind.Array)
                {
                    errors = JsonErrorParser.ParseErrors(errorsProp);
                }
            }
        }
        catch (Exception ex)
        {
            exception = ex;
        }

        return new OperationResult<TResultData>(
            data,
            dataInfo,
            ResultDataFactory,
            errors,
            MergeExtensions(response.Extensions, result.Extensions),
            MergeExtensions(response.ContextData, result.ContextData));
    }

    protected abstract IOperationResultDataInfo PatchData(PatchContext context);

    private static IReadOnlyDictionary<string, object?>? MergeExtensions(
        IReadOnlyDictionary<string, object?>? source,
        IReadOnlyDictionary<string, object?>? target)
    {
        if (source is null)
        {
            return target;
        }

        if (target is null)
        {
            return source;
        }

        var merged = new Dictionary<string, object?>(target);

        foreach (var (key, value) in source)
        {
            merged[key] = value;
        }

        return merged;
    }

    private static string CreatePath(JsonElement pathProp)
    {
        var pathBuilder = new StringBuilder();

        foreach (JsonElement segment in pathProp.EnumerateArray())
        {
            if (segment.ValueKind is JsonValueKind.String)
            {
                pathBuilder.Append(segment.GetString());
            }
            else if (segment.ValueKind is JsonValueKind.Number)
            {
                pathBuilder.Append(segment.GetInt32());
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        return pathBuilder.ToString();
    }

    protected readonly struct PatchContext
    {
        public readonly string Path;

        public readonly JsonElement DataProp;

        public readonly HashSet<EntityId> EntityIds;

        public readonly Dictionary<string, EntityId> PathToEntityIds;

        public PatchContext(
            string path,
            JsonElement dataProp,
            HashSet<EntityId> entityIds,
            Dictionary<string, EntityId> pathToEntityIds)
        {
            Path = path;
            DataProp = dataProp;
            EntityIds = entityIds;
            PathToEntityIds = pathToEntityIds;
        }
    }
}
