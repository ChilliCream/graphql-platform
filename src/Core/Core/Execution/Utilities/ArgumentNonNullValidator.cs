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
                    return new Report(type, path);
                }
                return default;
            }

            IType innerType = type.InnerType();

            if (innerType is ListType listType)
            {
                if (value is ListValueNode)
                {
                    return ValidateList(listType, (ListValueNode)value, path);
                }
                else
                {
                    Validate(listType.ElementType, value, path);
                }
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

        internal ref struct Report
        {
            internal Report(IType type, Path path)
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
