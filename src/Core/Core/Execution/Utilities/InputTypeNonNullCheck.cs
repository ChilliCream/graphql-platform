using System.Xml.Schema;
using System.Threading;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Types;
using HotChocolate.Utilities;

namespace HotChocolate.Execution
{
    internal static class InputTypeNonNullCheck
    {
        private static ThreadLocal<HashSet<object>> _processed =
            new ThreadLocal<HashSet<object>>(() => new HashSet<object>());

        public static IError CheckForNullValueViolation(
            NameString argumentName,
            IType type,
            object value,
            ITypeConversion converter,
            Func<string, IError> createError)
        {
            if (type is NonNullType && value == null)
            {
                return createError(string.Format(
                    CultureInfo.InvariantCulture,
                    TypeResources.ArgumentValueBuilder_NonNull,
                    argumentName,
                    TypeVisualizer.Visualize(type)));
            }

            var processed = _processed.Value;
            processed.Clear();

            return CheckForNullValueViolation(
                type, value, processed, converter, createError);
        }

        public static void CheckForNullValueViolation(
            IType type,
            object value,
            ITypeConversion converter,
            Func<string, IError> createError)
        {
            var processed = _processed.Value;
            processed.Clear();

            IError error = CheckForNullValueViolation(
                type, value, processed, converter, createError);

            if (error != null)
            {
                throw new QueryException(error);
            }
        }

        private static IError CheckForNullValueViolation(
            IType type,
            object value,
            ISet<object> processed,
            ITypeConversion converter,
            Func<string, IError> createError)
        {
            if (value is null)
            {
                if (type.IsNonNullType())
                {
                    return CreateError(createError, type);
                }
                return null;
            }

            if (type.IsListType())
            {
                return CheckForNullListViolation(
                    type.ListType(),
                    value,
                    processed,
                    converter,
                    createError);
            }

            if (type.IsInputObjectType()
                && type.NamedType() is InputObjectType t)
            {
                return CheckForNullFieldViolation(
                    t,
                    value,
                    processed,
                    converter,
                    createError);
            }

            return null;
        }

        private static IError CheckForNullFieldViolation(
            InputObjectType type,
            object value,
            ISet<object> processed,
            ITypeConversion converter,
            Func<string, IError> createError)
        {
            if (!processed.Add(value))
            {
                return null;
            }

            foreach (InputField field in type.Fields)
            {
                object obj = (type.ClrType != null
                    && !type.ClrType.IsInstanceOfType(value)
                    && converter.TryConvert(typeof(object), type.ClrType,
                        value, out object converted))
                    ? converted
                    : value;

                object fieldValue = field.GetValue(obj);
                IError error = CheckForNullValueViolation(
                    field.Type, fieldValue, processed,
                    converter, createError);

                if (error != null)
                {
                    return error;
                }
            }

            return null;
        }

        private static IError CheckForNullListViolation(
            ListType type,
            object value,
            ISet<object> processed,
            ITypeConversion converter,
            Func<string, IError> createError)
        {
            IType elementType = type.ElementType();

            foreach (object item in (IEnumerable)value)
            {
                IError error = CheckForNullValueViolation(
                    elementType, item, processed,
                    converter, createError);

                if (error != null)
                {
                    return error;
                }
            }

            return null;
        }

        private static IError CreateError(
            Func<string, IError> createError,
            IType type)
        {
            return createError(string.Format(
                CultureInfo.InvariantCulture,
                TypeResources.InputTypeNonNullCheck_ValueIsNull,
                TypeVisualizer.Visualize(type)));
        }
    }
}
