using System.Linq;
using System.Threading;
using System;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Execution
{
    internal static class NonNullValidator
    {
        private static ThreadLocal<List<object>> _path =
            new ThreadLocal<List<object>>(() => new List<object>());

        public static NonNullValidationReport Validate(
            IType type, ObjectValueNode value)
        {
            if (type is NonNullType && value == null)
            {
                return new NonNullValidationReport(type, null);
            }

            var path = _path.Value;
            path.Clear();

            try
            {
                return CheckForNullValueViolation(
                    type, value, path);
            }
            finally
            {
                path.Clear();
            }
        }

        private static NonNullValidationReport CheckForNullValueViolation(
            IType type,
            IValueNode value,
            IList<object> path)
        {
            if (value.IsNull())
            {
                if (type.IsNonNullType())
                {
                    return new NonNullValidationReport(type, CreatePath(path));
                }
                return default;
            }

            if (type.IsListType())
            {
                return CheckForNullListViolation(type.ListType(), value, path);
            }

            if (type.IsInputObjectType() && type.NamedType() is InputObjectType t)
            {
                return CheckForNullFieldViolation(t, value, path);
            }

            return default;
        }

        private static NonNullValidationReport CheckForNullFieldViolation(
            InputObjectType type,
            IValueNode value,
            IList<object> path)
        {
            if (value is ObjectValueNode objectValue)
            {
                Dictionary<string, IValueNode> dict =
                    objectValue.Fields.ToDictionary(t => t.Name.Value, t => t.Value);

                foreach (InputField field in type.Fields)
                {
                    if (!dict.TryGetValue(field.Name, out IValueNode fieldValue))
                    {
                        fieldValue = field.DefaultValue;
                    }

                    path.Push(field.Name);

                    NonNullValidationReport report = CheckForNullValueViolation(
                        field.Type, fieldValue, path);

                    if (report.HasError)
                    {
                        return report;
                    }

                    path.Pop();
                }
            }

            return default;
        }

        private static NonNullValidationReport CheckForNullListViolation(
            ListType type,
            IValueNode value,
            IList<object> path)
        {
            if (value is ListValueNode list)
            {
                for (int i = 0; i < list.Items.Count; i++)
                {
                    path.Push(i);

                    NonNullValidationReport report = CheckForNullValueViolation(
                        type.ElementType, list.Items[i], path);

                    if (report.HasError)
                    {
                        return report;
                    }

                    path.Pop();
                }
            }

            return default;
        }

        private static IReadOnlyList<object> CreatePath(IList<object> path)
        {
            if (path.Count == 0)
            {
                return Array.Empty<object>();
            }

            var copy = new object[path.Count];

            for (int i = 0; i < path.Count; i++)
            {
                copy[i] = path[i];
            }

            return copy;
        }
    }

    public readonly ref struct NonNullValidationReport
    {
        public NonNullValidationReport(
            IType type,
            IReadOnlyList<object> inputPath)
        {
            HasError = true;
            Type = type;
            InputPath = inputPath;
        }

        public bool HasError { get; }

        public IType Type { get; }

        public IReadOnlyList<object> InputPath { get; }
    }
}
