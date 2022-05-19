using System;
using System.Collections.Generic;
using System.Text.Json;
using StrawberryShake.Json;
using static StrawberryShake.ResultFields;

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
    protected abstract IOperationResultDataFactory<TResultData> ResultDataFactory { get; }

    public IOperationResult<TResultData> Build(
        Response<JsonDocument> response)
    {
        TResultData? data = null;
        IOperationResultDataInfo? dataInfo = null;
        IReadOnlyList<IClientError>? errors = null;

        try
        {
            if (response.Body is { } body)
            {
                if (body.RootElement.TryGetProperty(Data, out JsonElement dataProp) &&
                    dataProp.ValueKind is JsonValueKind.Object)
                {
                    dataInfo = BuildData(dataProp);
                    data = ResultDataFactory.Create(dataInfo);
                }

                if (body.RootElement.TryGetProperty(Errors, out JsonElement errorsProp) &&
                    errorsProp.ValueKind is JsonValueKind.Array)
                {
                    errors = JsonErrorParser.ParseErrors(errorsProp);
                }
            }
        }
        catch (Exception ex)
        {
            var list = new List<IClientError>
            {
                new ClientError(
                ex.Message,
                ErrorCodes.InvalidResultDataStructure,
                exception: ex,
                extensions: new Dictionary<string, object?>
                {
                    { nameof(ex.StackTrace), ex.StackTrace }
                })
            };

            if (errors is not null)
            {
                list.AddRange(errors);
            }

            errors = list;
        }

        return new OperationResult<TResultData>(
            data,
            dataInfo,
            ResultDataFactory,
            errors,
            response.Extensions,
            response.ContextData);
    }

    protected abstract IOperationResultDataInfo BuildData(JsonElement obj);
}
