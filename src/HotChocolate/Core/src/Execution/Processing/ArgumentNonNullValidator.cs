using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Execution.Processing
{
    internal static class ArgumentNonNullValidator
    {
        public static ValidationResult Validate(IInputField field, IValueNode? value, Path path)
        {
            if (value.IsNull())
            {
                if (field.Type.IsNonNullType() && field.DefaultValue.IsNull())
                {
                    return new ValidationResult(field.Type, path);
                }
                return default;
            }

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
            IType innerType = type.IsNonNullType() ? type.InnerType() : type;

            if (innerType is ListType listType)
            {
                if (value is ListValueNode listValue)
                {
                    return ValidateList(listType, listValue, path);
                }
                else
                {
                    Validate(listType.ElementType, value, path);
                }
            }

            if (innerType is InputObjectType inputType && value is ObjectValueNode ov)
            {
                return ValidateObject(inputType, ov, path);
            }

            return default;
        }

        private static ValidationResult ValidateObject(InputObjectType type, ObjectValueNode value, Path path)
        {
            var fields = new Dictionary<NameString, IValueNode>();

            for (int i = 0; i < value.Fields.Count; i++)
            {
                ObjectFieldNode field = value.Fields[i];
                fields[field.Name.Value] = field.Value;
            }

            foreach (InputField field in type.Fields)
            {
                fields.TryGetValue(field.Name, out IValueNode? fieldValue);

                ValidationResult report = Validate(
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
            IType elementType = type.ElementType();
            int i = 0;

            foreach (IValueNode element in list.Items)
            {
                ValidationResult error = Validate(elementType, element, path.Append(i++));
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
}
