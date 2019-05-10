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
        public static IError CheckForNullValueViolation(
            NameString argumentName,
            IType type,
            object value,
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

            return CheckForNullValueViolation(
                type, value, new HashSet<object>(), createError);
        }

        public static void CheckForNullValueViolation(
            IType type,
            object value,
            Func<string, IError> createError)
        {
            IError error = CheckForNullValueViolation(
                type, value, new HashSet<object>(), createError);

            if (error != null)
            {
                throw new QueryException(error);
            }
        }

        private static IError CheckForNullValueViolation(
            IType type,
            object value,
            ISet<object> processed,
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
                    createError);
            }

            if (type.IsInputObjectType()
                && type.NamedType() is InputObjectType t)
            {
                return CheckForNullFieldViolation(
                    t,
                    value,
                    processed,
                    createError);
            }

            return null;
        }

        private static IError CheckForNullFieldViolation(
            InputObjectType type,
            object value,
            ISet<object> processed,
            Func<string, IError> createError)
        {
            if (!processed.Add(value))
            {
                return null;
            }

            foreach (InputField field in type.Fields)
            {
                object fieldValue = field.GetValue(value);
                IError error = CheckForNullValueViolation(
                    field.Type, fieldValue, processed, createError);
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
            Func<string, IError> createError)
        {
            IType elementType = type.ElementType();

            foreach (object item in (IEnumerable)value)
            {
                IError error = CheckForNullValueViolation(
                    elementType, item, processed, createError);
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
