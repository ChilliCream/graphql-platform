using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Data.Filters;

/// <summary>
/// Represents a collection of filter fields and operations .
/// </summary>
public class FilterInfo : FilterValueNode, IFilterInfo
{
    private IReadOnlyList<IFilterFieldInfo>? _fieldInfos;
    private IReadOnlyList<IFilterOperationInfo>? _operationInfos;

    private readonly InputParser _inputParser;

    /// <summary>
    /// Initializes a new instance of <see cref="FilterInfo"/>
    /// </summary>
    public FilterInfo(IType type, IValueNode valueNode, InputParser inputParser)
        : base(type, valueNode)
    {
        _inputParser = inputParser;
    }

    /// <inheritdoc />
    public IReadOnlyList<IFilterFieldInfo> GetFields()
    {
        Initialize();
        return _fieldInfos!;
    }

    /// <inheritdoc />
    public IReadOnlyList<IFilterOperationInfo> GetOperations()
    {
        Initialize();
        return _operationInfos!;
    }

    private void Initialize()
    {
        if (_fieldInfos is not null && _operationInfos is not null)
        {
            return;
        }

        var type = Type;
        if (Type is NonNullType nonNullType)
        {
            type = nonNullType.Type;
        }

        if (ValueNode is ObjectValueNode valueNode &&
            type is FilterInputType filterInputType)
        {
            List<IFilterFieldInfo>? fieldInfos = null;
            List<IFilterOperationInfo>? operationInfos = null;
            foreach (var fieldValue in valueNode.Fields)
            {
                if (filterInputType.Fields.TryGetField(fieldValue.Name.Value, out var field))
                {
                    var value = CreateValueInfo(fieldValue.Value, field.Type);
                    switch (field)
                    {
                        case FilterOperationField operationField:
                            operationInfos ??= [];
                            operationInfos.Add(new FilterOperationInfo(operationField, value));
                            break;

                        case FilterField filterField:
                            fieldInfos ??= [];
                            fieldInfos.Add(new FilterFieldInfo(filterField, value));
                            break;
                    }
                }
            }

            _fieldInfos = fieldInfos;
            _operationInfos = operationInfos;
        }

        _fieldInfos ??= Array.Empty<IFilterFieldInfo>();
        _operationInfos ??= Array.Empty<IFilterOperationInfo>();
    }

    private IFilterValueNode CreateValueInfo(IValueNode valueNode, IType type)
    {
        var normalizedType = type;
        if (type is NonNullType nonNullType)
        {
            normalizedType = nonNullType.Type;
        }

        if (valueNode is ListValueNode listValueNode &&
            normalizedType.IsListType() &&
            normalizedType.NamedType() is IFilterInputType)
        {
            List<IFilterValueNode> values = new(listValueNode.Items.Count);

            foreach (var item in listValueNode.Items)
            {
                values.Add(CreateValueInfo(item, normalizedType.ElementType()));
            }

            return new FilterValueCollection(type, valueNode, values);
        }
        else if (normalizedType is IFilterInputType)
        {
            return new FilterInfo(type, valueNode, _inputParser);
        }
        else
        {
            return new FilterValue(type, valueNode, _inputParser);
        }
    }
}
