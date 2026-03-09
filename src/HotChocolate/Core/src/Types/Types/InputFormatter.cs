using System.Collections;
using HotChocolate.Language;
using HotChocolate.Utilities;
using static HotChocolate.Utilities.ThrowHelper;

namespace HotChocolate.Types;

public sealed class InputFormatter
{
    private readonly ITypeConverter _converter;

    public InputFormatter(ITypeConverter converter)
    {
        ArgumentNullException.ThrowIfNull(converter);

        _converter = converter;
    }

    public InputFormatter() : this(new DefaultTypeConverter()) { }

    public IValueNode FormatValue(object? runtimeValue, IType type, Path? path = null)
    {
        ArgumentNullException.ThrowIfNull(type);

        return FormatValueInternal(runtimeValue, type, path ?? Path.Root);
    }

    private IValueNode FormatValueInternal(object? runtimeValue, IType type, Path path)
    {
        if (runtimeValue is null)
        {
            if (type.Kind == TypeKind.NonNull)
            {
                throw NonNullInputViolation(type, path);
            }

            return NullValueNode.Default;
        }

        switch (type.Kind)
        {
            case TypeKind.NonNull:
                return FormatValueInternal(runtimeValue, ((NonNullType)type).NullableType, path);

            case TypeKind.List:
                return FormatValueList(runtimeValue, (ListType)type, path);

            case TypeKind.InputObject:
                return FormatValueObject(runtimeValue, (InputObjectType)type, path);

            case TypeKind.Enum:
            case TypeKind.Scalar:
                return FormatValueLeaf(runtimeValue, (ILeafType)type, path);

            default:
                throw new NotSupportedException();
        }
    }

    private ObjectValueNode FormatValueObject(object runtimeValue, InputObjectType type, Path path)
    {
        var fields = new List<ObjectFieldNode>();
        var fieldValues = new object?[type.Fields.Count];
        type.GetFieldValues(runtimeValue, fieldValues);

        for (var i = 0; i < fieldValues.Length; i++)
        {
            var field = type.Fields[i];
            var fieldValue = fieldValues[i];
            var fieldPath = path.Append(field.Name);

            if (field.IsOptional)
            {
                var optional = (IOptional)fieldValue!;
                if (optional.HasValue)
                {
                    AddField(optional.Value, field.Name, field.Type, fieldPath);
                }
            }
            else
            {
                AddField(fieldValue, field.Name, field.Type, fieldPath);
            }
        }

        return new ObjectValueNode(fields);

        void AddField(object? fieldValue, string fieldName, IInputType fieldType, Path fieldPath)
        {
            var value = FormatValueInternal(fieldValue, fieldType, fieldPath);
            fields.Add(new ObjectFieldNode(fieldName, value));
        }
    }

    private ListValueNode FormatValueList(object runtimeValue, ListType type, Path path)
    {
        if (runtimeValue is IList runtimeList)
        {
            var items = new List<IValueNode>();

            for (var i = 0; i < runtimeList.Count; i++)
            {
                var newPath = path.Append(i);
                items.Add(FormatValueInternal(runtimeList[i], type.ElementType, newPath));
            }

            return new ListValueNode(items);
        }

        if (runtimeValue is IEnumerable enumerable)
        {
            var items = new List<IValueNode>();
            const int i = 0;

            foreach (var item in enumerable)
            {
                var newPath = path.Append(i);
                items.Add(FormatValueInternal(item, type.ElementType, newPath));
            }

            return new ListValueNode(items);
        }

        throw FormatValueList_InvalidObjectKind(type, runtimeValue.GetType(), path);
    }

    private IValueNode FormatValueLeaf(object runtimeValue, ILeafType type, Path path)
    {
        try
        {
            var runtimeType = type.RuntimeType;

            if (runtimeValue.GetType() != runtimeType
                && _converter.TryConvert(runtimeType, runtimeValue, out var converted))
            {
                runtimeValue = converted;
            }

            return type.ValueToLiteral(runtimeValue);
        }
        catch (LeafCoercionException ex)
        {
            throw new LeafCoercionException(ex.Errors[0], ex.Type, path);
        }
    }

    public DirectiveNode FormatDirective(object runtimeValue, DirectiveType type, Path? path = null)
    {
        ArgumentNullException.ThrowIfNull(runtimeValue);
        ArgumentNullException.ThrowIfNull(type);

        path ??= Path.Root.Append(type.Name);

        var fields = new List<ArgumentNode>();
        var fieldValues = new object?[type.Arguments.Count];
        type.GetFieldValues(runtimeValue, fieldValues);

        for (var i = 0; i < fieldValues.Length; i++)
        {
            var field = type.Arguments[i];
            var fieldValue = fieldValues[i];
            var fieldPath = path.Append(field.Name);

            if (field.IsOptional)
            {
                var optional = (IOptional)fieldValue!;
                if (optional.HasValue)
                {
                    AddField(optional.Value, field.Name, field.Type, fieldPath);
                }
            }
            else
            {
                AddField(fieldValue, field.Name, field.Type, fieldPath);
            }
        }

        return new DirectiveNode(type.Name, fields);

        void AddField(object? fieldValue, string fieldName, IInputType fieldType, Path fieldPath)
        {
            var value = FormatValueInternal(fieldValue, fieldType, fieldPath);
            fields.Add(new ArgumentNode(fieldName, value));
        }
    }
}
