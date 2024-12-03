using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Execution.Processing;

internal static class ArgumentNonNullValidator
{
    public static ValidationResult Validate(IInputField field, IValueNode? value, Path path)
    {
        // if no value was provided
        if (value is null)
        {
            // the type is a non-null type and no default value has been set we mark this
            // field as violation.
            if (field.Type.IsNonNullType() && field.DefaultValue.IsNull())
            {
                return new ValidationResult(field.Type, path);
            }

            // if the field has a default value or nullable everything is fine and we
            // return success.
            return default;
        }

        // if null was explicitly set
        if (value is NullValueNode)
        {
            // and the field type is a non-null type we will mark the field value
            // as violation.
            if (field.Type.IsNonNullType())
            {
                return new ValidationResult(field.Type, path);
            }

            // if the field is nullable we will mark the field as valid.
            return default;
        }

        // if the field has a value we traverse it and make sure the value is correct.
        return ValidateInnerType(field.Type, value, path);
    }

    private static ValidationResult Validate(IType type, IValueNode? value, Path path)
    {
        if (value.IsNull())
        {
            return type.IsNonNullType() ? new ValidationResult(type, path) : default;
        }

        return ValidateInnerType(type, value, path);
    }

    private static ValidationResult ValidateInnerType(IType type, IValueNode? value, Path path)
    {
        var innerType = type.IsNonNullType() ? type.InnerType() : type;

        if (innerType is ListType listType)
        {
            if (value is ListValueNode listValue)
            {
                return ValidateList(listType, listValue, path);
            }

            Validate(listType.ElementType, value, path);
        }

        if (innerType is InputObjectType inputType && value is ObjectValueNode ov)
        {
            return ValidateObject(inputType, ov, path);
        }

        return default;
    }

    private static ValidationResult ValidateObject(
        InputObjectType type,
        ObjectValueNode value,
        Path path)
    {
        var fields = new Dictionary<string, IValueNode>(StringComparer.Ordinal);

        for (var i = 0; i < value.Fields.Count; i++)
        {
            var field = value.Fields[i];
            fields[field.Name.Value] = field.Value;
        }

        foreach (var field in type.Fields)
        {
            fields.TryGetValue(field.Name, out var fieldValue);

            var report = Validate(
                field,
                fieldValue,
                path.Append(field.Name));

            if (report.HasErrors)
            {
                return report;
            }
        }

        return default;
    }

    private static ValidationResult ValidateList(ListType type, ListValueNode list, Path path)
    {
        var elementType = type.ElementType();
        var i = 0;

        foreach (var element in list.Items)
        {
            var error = Validate(elementType, element, path.Append(i++));
            if (error.HasErrors)
            {
                return error;
            }
        }

        return default;
    }

    internal readonly ref struct ValidationResult
    {
        internal ValidationResult(IType type, Path path)
        {
            Type = type;
            Path = path;
            HasErrors = true;
        }

        public bool HasErrors { get; }

        public Path Path { get; }

        public IType Type { get; }
    }
}
