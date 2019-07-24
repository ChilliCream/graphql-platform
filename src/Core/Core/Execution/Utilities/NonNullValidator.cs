using System.Threading;
using System;
using System.Collections;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Utilities;

namespace HotChocolate.Execution
{
    internal static class NonNullValidator
    {
        private static ThreadLocal<HashSet<object>> _processed =
            new ThreadLocal<HashSet<object>>(() => new HashSet<object>());

        private static ThreadLocal<List<object>> _path =
            new ThreadLocal<List<object>>(() => new List<object>());

        public static NonNullValidationReport Validate(
            IType type,
            object value,
            ITypeConversion converter)
        {
            if (type is NonNullType && value == null)
            {
                return new NonNullValidationReport(type, null);
            }

            var processed = _processed.Value;
            processed.Clear();

            var path = _path.Value;
            path.Clear();

            try
            {
                return CheckForNullValueViolation(
                    type, value, processed, path, converter);
            }
            finally
            {
                processed.Clear();
                path.Clear();
            }
        }

        private static NonNullValidationReport CheckForNullValueViolation(
            IType type,
            object value,
            ISet<object> processed,
            IList<object> path,
            ITypeConversion converter)
        {
            if (value is null)
            {
                if (type.IsNonNullType())
                {
                    return new NonNullValidationReport(type, CreatePath(path));
                }
                return default;
            }

            if (type.IsListType())
            {
                return CheckForNullListViolation(
                    type.ListType(),
                    value,
                    processed,
                    path,
                    converter);
            }

            if (type.IsInputObjectType()
                && type.NamedType() is InputObjectType t)
            {
                return CheckForNullFieldViolation(
                    t,
                    value,
                    processed,
                    path,
                    converter);
            }

            return default;
        }

        private static NonNullValidationReport CheckForNullFieldViolation(
            InputObjectType type,
            object value,
            ISet<object> processed,
            IList<object> path,
            ITypeConversion converter)
        {
            if (!processed.Add(value))
            {
                return default;
            }

            object obj = (type.ClrType != null
                && !type.ClrType.IsInstanceOfType(value)
                && converter.TryConvert(typeof(object), type.ClrType,
                    value, out object converted))
                ? converted
                : value;

            foreach (InputField field in type.Fields)
            {
                path.Push(field.Name);

                object fieldValue = field.GetValue(obj);
                NonNullValidationReport report = CheckForNullValueViolation(
                    field.Type, fieldValue, processed, path, converter);

                if (report.HasError)
                {
                    return report;
                }

                path.Pop();
            }

            return default;
        }

        private static NonNullValidationReport CheckForNullListViolation(
            ListType type,
            object value,
            ISet<object> processed,
            IList<object> path,
            ITypeConversion converter)
        {
            IType elementType = type.ElementType();
            int i = 0;

            foreach (object item in (IEnumerable)value)
            {
                path.Push(i++);

                NonNullValidationReport report = CheckForNullValueViolation(
                    elementType, item, processed, path, converter);

                if (report.HasError)
                {
                    return report;
                }

                path.Pop();
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
