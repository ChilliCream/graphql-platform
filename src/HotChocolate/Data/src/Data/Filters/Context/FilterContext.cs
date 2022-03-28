using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Data.Filters;
public class FilterContext : IFilterContext
{
    private IReadOnlyList<IFilterFieldInfo>? _fieldInfos;
    private IReadOnlyList<IFilterOperationInfo>? _operationInfos;

    private readonly InputParser _inputParser;
    private readonly IType? _type;

    public FilterContext(IValueNode valueNode, IType type, InputParser inputParser)
    {
        ValueNode = valueNode;
        _type = type;
        _inputParser = inputParser;
    }

    public IValueNode ValueNode { get; }

    public IFilterMemberInfo? Parent { get; }

    public IReadOnlyList<IFilterFieldInfo> GetFields()
    {
        Initialize();
        return _fieldInfos;
    }

    public IReadOnlyList<IFilterOperationInfo> GetOperations()
    {
        Initialize();
        return _operationInfos;
    }

    public IDictionary<string, object?> ToDictionary()
    {
        Initialize();
        Dictionary<string, object?> data = new();

        foreach (var field in GetFields())
        {
            data[field.Field.Name.Value] = field.ToDictionary();
        }

        foreach (var operation in GetOperations())
        {
            data[operation.Field.Name.Value] = operation.Value;
        }

        return data;
    }

    [MemberNotNull(nameof(_fieldInfos))]
    [MemberNotNull(nameof(_operationInfos))]
    private void Initialize()
    {
        if (_fieldInfos is not null && _operationInfos is not null)
        {
            return;
        }

        if (ValueNode is ObjectValueNode valueNode &&
            _type is FilterInputType filterInputType)
        {
            var fieldInfos = new List<IFilterFieldInfo>();
            var operationInfos = new List<IFilterOperationInfo>();
            foreach (var fieldValue in valueNode.Fields)
            {
                var field = filterInputType.Fields
                    .FirstOrDefault(x => x.Name.Value == fieldValue.Name.Value);
                if (fieldValue is { })
                {
                    if (field is FilterOperationField operationField)
                    {
                        FilterContext context = new(fieldValue.Value, field.Type, _inputParser);
                        FilterOperationInfo info = new(
                            context,
                            this,
                            operationField,
                            _inputParser.ParseLiteral(fieldValue.Value, field));

                        operationInfos.Add(info);
                    }
                    else if (field is FilterField filterField)
                    {
                        FilterFieldInfo info = new(
                            fieldValue.Value,
                            field.Type,
                            _inputParser,
                            this,
                            filterField);
                        fieldInfos.Add(info);
                    }
                }
            }
            _fieldInfos = fieldInfos;
            _operationInfos = operationInfos;
        }
        else
        {
            _fieldInfos = Array.Empty<IFilterFieldInfo>();
            _operationInfos = Array.Empty<IFilterOperationInfo>();
        }
    }
}
