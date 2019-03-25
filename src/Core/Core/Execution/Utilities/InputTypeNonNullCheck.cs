using System;
using System.Collections;
using System.Collections.Generic;
using HotChocolate;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Execution
{
    internal class InputTypeNonNullCheck
    {
        public static void CheckForNullValueViolation(
            IType type,
            object value,
            Func<string, IError> createError)
        {
            if (type.IsNonNullType() && value is null)
            {
                throw CreateError(type, value, createError);
            }

            CheckForNullValueViolation(
                type, value, new HashSet<object>(), createError);
        }

        public static void CheckForNullValueViolation(
            IType type,
            object value,
            ISet<object> processed,
            Func<string, IError> createError)
        {
            if (type.IsNonNullType() && value is null)
            {
                throw CreateError(syntaxNode, type, value);
            }

            if (type.IsListType())
            {
                CheckForNullListViolation(
                    syntaxNode,
                    type.ListType(),
                    value,
                    processed);
            }

            if (type.IsInputObjectType()
                && type.NamedType() is InputObjectType t)
            {
                CheckForNullFieldViolation(
                    syntaxNode,
                    t,
                    value,
                    processed);
            }
        }

        private static void CheckForNullFieldViolation(
            ISyntaxNode syntaxNode,
            InputObjectType type,
            object value,
            ISet<object> processed)
        {
            if (!processed.Add(value))
            {
                return;
            }

            foreach (InputField field in type.Fields)
            {
                object fieldValue = field.GetValue(value);
                CheckForNullValueViolation(
                    syntaxNode, field.Type, fieldValue, processed);
            }
        }

        private static void CheckForNullListViolation(
            ISyntaxNode syntaxNode,
            ListType type,
            object value,
            ISet<object> processed)
        {
            IType elementType = type.ElementType();

            foreach (object item in (IEnumerable)value)
            {
                CheckForNullValueViolation(
                    syntaxNode, elementType, item, processed);
            }
        }

        private static QueryException CreateError(
            Func<string, IError> createError)
        {
            // TODO : resources
            return new QueryException(
                createError("The input value cannot be null."));
        }
    }
}
