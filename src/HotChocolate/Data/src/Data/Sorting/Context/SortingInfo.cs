using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Data.Sorting;

/// <summary>
/// Represents a collection of sorting fields and operations .
/// </summary>
public class SortingInfo : SortingValueNode, ISortingInfo
{
    private IReadOnlyList<ISortingFieldInfo>? _fieldInfos;

    private readonly InputParser _inputParser;

    /// <summary>
    /// Creates a new instance of <see cref="SortingInfo"/>
    /// </summary>
    public SortingInfo(IType type, IValueNode valueNode, InputParser inputParser)
        : base(type, valueNode)
    {
        _inputParser = inputParser;
    }

    /// <inheritdoc />
    public IReadOnlyList<ISortingFieldInfo> GetFields()
    {
        Initialize();
        return _fieldInfos!;
    }

    private void Initialize()
    {
        if (_fieldInfos is not null)
        {
            return;
        }

        var type = Type;
        if (Type is NonNullType nonNullType)
        {
            type = nonNullType.Type;
        }

        if (ValueNode is ObjectValueNode valueNode &&
            type is SortInputType sortingInputType)
        {
            List<ISortingFieldInfo>? fieldInfos = null;
            foreach (var fieldValue in valueNode.Fields)
            {
                if (sortingInputType.Fields.TryGetField(fieldValue.Name.Value, out var field))
                {
                    var value = CreateValueInfo(fieldValue.Value, field.Type);
                    if (field is SortField fieldInfo)
                    {
                        fieldInfos ??= [];
                        fieldInfos.Add(new SortingFieldInfo(fieldInfo, value));
                    }
                }
            }

            _fieldInfos = fieldInfos;
        }

        _fieldInfos ??= Array.Empty<ISortingFieldInfo>();
    }

    private ISortingValueNode CreateValueInfo(IValueNode valueNode, IType type)
    {
        var normalizedType = type;
        if (type is NonNullType nonNullType)
        {
            normalizedType = nonNullType.Type;
        }

        if (valueNode is ListValueNode listValueNode &&
            normalizedType.IsListType() &&
            normalizedType.NamedType() is ISortInputType)
        {
            List<ISortingValueNode> values = new(listValueNode.Items.Count);

            foreach (var item in listValueNode.Items)
            {
                values.Add(CreateValueInfo(item, normalizedType.ElementType()));
            }

            return new SortingValueCollection(type, valueNode, values);
        }
        else if (normalizedType is ISortInputType)
        {
            return new SortingInfo(type, valueNode, _inputParser);
        }
        else
        {
            return new SortingValue(type, valueNode, _inputParser);
        }
    }
}
