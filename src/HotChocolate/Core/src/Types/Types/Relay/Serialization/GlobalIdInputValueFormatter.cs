using System;
using System.Buffers;
using System.Collections.Generic;

#nullable enable

namespace HotChocolate.Types.Relay;

internal class GlobalIdInputValueFormatter(
    INodeIdSerializerAccessor serializerAccessor,
    Type runtimeType,
    Type elementType,
    string name,
    bool validateTypeName)
    : IInputValueFormatter
{
    private INodeIdSerializer? _serializer;

    public object? Format(object? originalValue)
    {
        if (originalValue is null)
        {
            return null;
        }

        return FormatInternal(originalValue);
    }

    private object FormatInternal(object originalValue)
    {
        _serializer ??= serializerAccessor.Serializer;

        switch (originalValue)
        {
            case NodeId nodeId:
            {
                ValidateTypeName(nodeId.TypeName);
                return nodeId.InternalId;
            }

            case string formattedId:
            {
                var nodeId = _serializer.Parse(formattedId, runtimeType);
                ValidateTypeName(nodeId.TypeName);
                return nodeId.InternalId;
            }

            case IReadOnlyList<NodeId> nodeIds:
            {
                var internalIds = Array.CreateInstance(elementType, nodeIds.Count);

                for (var i = 0; i < nodeIds.Count; i++)
                {
                    var nodeId = nodeIds[i];
                    ValidateTypeName(nodeId.TypeName);
                    internalIds.SetValue(nodeId.InternalId, i);
                }

                return internalIds;
            }

            case IReadOnlyList<string?> formattedIds:
            {
                var internalIds = Array.CreateInstance(elementType, formattedIds.Count);

                for (var i = 0; i < formattedIds.Count; i++)
                {
                    if (formattedIds[i] is { } formattedId)
                    {
                        var nodeId = _serializer.Parse(formattedId, runtimeType);
                        ValidateTypeName(nodeId.TypeName);
                        internalIds.SetValue(nodeId.InternalId, i);
                    }
                }

                return internalIds;
            }

            case IEnumerable<NodeId> nodeIds:
            {
                var tempIds = ArrayPool<object>.Shared.Rent(128);

                var i = 0;
                foreach (var nodeId in nodeIds)
                {
                    ValidateTypeName(nodeId.TypeName);
                    tempIds[i++] = nodeId.InternalId;

                    if (i == tempIds.Length)
                    {
                        var buffer = ArrayPool<object>.Shared.Rent(tempIds.Length * 2);
                        Array.Copy(tempIds, buffer, tempIds.Length);
                        ArrayPool<object>.Shared.Return(tempIds);
                        tempIds = buffer;
                    }
                }

                var internalIds = Array.CreateInstance(elementType, i);
                Array.Copy(tempIds, internalIds, i);
                ArrayPool<object>.Shared.Return(tempIds);
                return internalIds;
            }

            case IEnumerable<string?> formattedIds:
            {
                var tempIds = ArrayPool<object>.Shared.Rent(128);

                var i = 0;
                foreach (var formattedId in formattedIds)
                {
                    if (formattedId is null)
                    {
                        i++;
                        continue;
                    }

                    var nodeId = _serializer.Parse(formattedId, runtimeType);
                    ValidateTypeName(nodeId.TypeName);
                    tempIds[i++] = nodeId.InternalId;

                    if (i == tempIds.Length)
                    {
                        var buffer = ArrayPool<object>.Shared.Rent(tempIds.Length * 2);
                        Array.Copy(tempIds, buffer, tempIds.Length);
                        ArrayPool<object>.Shared.Return(tempIds);
                        tempIds = buffer;
                    }
                }

                var internalIds = Array.CreateInstance(elementType, i);
                Array.Copy(tempIds, internalIds, i);
                ArrayPool<object>.Shared.Return(tempIds);
                return internalIds;
            }
        }

        throw new ArgumentException("The format of the originalValue cannot be handled.", nameof(originalValue));
    }

    private void ValidateTypeName(string typeName)
    {
        if(validateTypeName && !string.Equals(name, typeName, StringComparison.Ordinal))
        {
            var error =
                ErrorBuilder.New()
                    .SetMessage(
                        "The node id type name `{0}` does not match the expected type name `{1}`.",
                        typeName,
                        name)
                    .Build();

            throw new GraphQLException(error);
        }
    }
}
