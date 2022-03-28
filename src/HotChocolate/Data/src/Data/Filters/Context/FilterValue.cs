using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Data.Filters;

/// <summary>
/// Represents a value of a filter.
/// </summary>
public class FilterValue : FilterValueInfo, IFilterValue
{
    private IReadOnlyList<IFilterFieldInfo>? _fieldInfos;
    private IReadOnlyList<IFilterOperationInfo>? _operationInfos;

    private readonly InputParser _inputParser;

    /// <summary>
    /// Creates a new instance of <see cref="FilterValue"/>
    /// </summary>
    public FilterValue(IType type, IValueNode valueNode, InputParser inputParser)
        : base(type, valueNode)
    {
        _inputParser = inputParser;
    }

    /// <inheritdoc />
    public object? ParseValue() => _inputParser.ParseLiteral(ValueNode, Type);

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

        IType type = Type;
        if (Type is NonNullType nonNullType)
        {
            type = nonNullType.Type;
        }

        if (ValueNode is ObjectValueNode valueNode &&
            type is FilterInputType filterInputType)
        {
            List<IFilterFieldInfo>? fieldInfos = null;
            List<IFilterOperationInfo>? operationInfos = null;
            foreach (ObjectFieldNode fieldValue in valueNode.Fields)
            {
                if (filterInputType.Fields.TryGetField(fieldValue.Name.Value, out var field))
                {
                    IFilterValueInfo value = CreateValueInfo(fieldValue.Value, field.Type);
                    switch (field)
                    {
                        case FilterOperationField operationField:
                            operationInfos ??= new();
                            operationInfos.Add(new FilterOperationInfo(operationField, value));
                            break;

                        case FilterField filterField:
                            fieldInfos ??= new();
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

    private IFilterValueInfo CreateValueInfo(IValueNode valueNode, IType type)
    {
        IType normalizedType = type;
        if (type is NonNullType nonNullType)
        {
            normalizedType = nonNullType.Type;
        }

        if (valueNode is ListValueNode listValueNode &&
            normalizedType.IsListType() &&
            !normalizedType.NamedType().IsScalarType())
        {
            List<IFilterValueInfo> values = new(listValueNode.Items.Count);

            foreach (IValueNode item in listValueNode.Items)
            {
                values.Add(CreateValueInfo(item, normalizedType.ElementType()));
            }

            return new FilterValueCollection(type, valueNode, values);
        }
        else
        {
            return new FilterValue(type, valueNode, _inputParser);
        }
    }
}
