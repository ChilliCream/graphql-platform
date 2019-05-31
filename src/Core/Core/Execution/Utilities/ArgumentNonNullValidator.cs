using System.Collections.Generic;
using System.Globalization;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Types;

namespace HotChocolate.Execution
{
    internal static class ArgumentNonNullValidator
    {
        public static Report Validate(IType type, IValueNode value, Path path)
        {
            if (value.IsNull())
            {
                if (type.IsNonNullType())
                {
                    return CreateError(type, path);
                }
                return default;
            }

            IType innerType = type.InnerType();

            if (innerType is ListType listType)
            {
                return ValidateList(listType, (ListValueNode)value, path);
            }

            if (innerType is InputObjectType inputType)
            {
                return ValidateObject(inputType, (ObjectValueNode)value, path);
            }

            return default;
        }


        private static Report ValidateObject(
            InputObjectType type,
            ObjectValueNode value,
            Path path)
        {
            var fields = new Dictionary<NameString, IValueNode>();

            for (int i = 0; i < value.Fields.Count; i++)
            {
                ObjectFieldNode field = value.Fields[i];
                fields[field.Name.Value] = field.Value;
            }

            foreach (InputField field in type.Fields)
            {
                fields.TryGetValue(field.Name, out IValueNode fieldValue);

                Report report = Validate(
                    field.Type,
                    fieldValue,
                    path.Append(field.Name));

                if (report.HasErrors)
                {
                    return report;
                }
            }

            return default;
        }

        private static Report ValidateList(
            ListType type, ListValueNode list, Path path)
        {
            IType elementType = type.ElementType();
            int i = 0;

            foreach (IValueNode element in list.Items)
            {
                Report error = Validate(elementType, element, path.Append(i++));
                if (error.HasErrors)
                {
                    return error;
                }
            }

            return default;
        }

        private static Report CreateError(IType type, Path path)
        {
            return new Report(path, string.Format(
                CultureInfo.InvariantCulture,
                TypeResources.InputTypeNonNullCheck_ValueIsNull,
                TypeVisualizer.Visualize(type)));
        }

        internal ref struct Report
        {
            internal Report(Path path, string message)
            {
                HasErrors = true;
                Path = path;
                Message = message;
            }

            public bool HasErrors { get; }
            public Path Path { get; }
            public string Message { get; }
        }
    }
}
