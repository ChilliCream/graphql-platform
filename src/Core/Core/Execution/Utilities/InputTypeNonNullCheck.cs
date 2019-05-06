using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Types;

namespace HotChocolate.Execution
{
    internal static class InputTypeNonNullCheck
    {

        public static void CheckForNullValueViolation(
            NameString argumentName,
            IType type,
            object value,
            Func<string, IError> createError)
        {
            if (type is NonNullType && value == null)
            {
                throw new QueryException(createError(string.Format(
                    CultureInfo.InvariantCulture,
                    TypeResources.ArgumentValueBuilder_NonNull,
                    argumentName,
                    TypeVisualizer.Visualize(type))));
            }

            CheckForNullValueViolation(type, value, createError);
        }

        public static void CheckForNullValueViolation(
            IType type,
            object value,
            Func<string, IError> createError)
        {
            CheckForNullValueViolation(
                type, value, new HashSet<object>(), createError);
        }

        private static void CheckForNullValueViolation(
            IType type,
            object value,
            ISet<object> processed,
            Func<string, IError> createError)
        {
            if (value is null)
            {
                if (type.IsNonNullType())
                {
                    throw CreateError(createError, type);
                }
                return;
            }

            if (type.IsListType())
            {
                CheckForNullListViolation(
                    type.ListType(),
                    value,
                    processed,
                    createError);
            }

            if (type.IsInputObjectType()
                && type.NamedType() is InputObjectType t)
            {
                CheckForNullFieldViolation(
                    t,
                    value,
                    processed,
                    createError);
            }
        }

        private static void CheckForNullFieldViolation(
            InputObjectType type,
            object value,
            ISet<object> processed,
            Func<string, IError> createError)
        {
            if (!processed.Add(value))
            {
                return;
            }

            foreach (InputField field in type.Fields)
            {
                object fieldValue = field.GetValue(value);
                CheckForNullValueViolation(
                    field.Type, fieldValue, processed, createError);
            }
        }

        private static void CheckForNullListViolation(
            ListType type,
            object value,
            ISet<object> processed,
            Func<string, IError> createError)
        {
            IType elementType = type.ElementType();

            foreach (object item in (IEnumerable)value)
            {
                CheckForNullValueViolation(
                    elementType, item, processed, createError);
            }
        }

        private static QueryException CreateError(
            Func<string, IError> createError,
            IType type)
        {
            return new QueryException(
                createError(string.Format(
                    CultureInfo.InvariantCulture,
                    TypeResources.InputTypeNonNullCheck_ValueIsNull,
                    TypeVisualizer.Visualize(type))));
        }
    }
}
