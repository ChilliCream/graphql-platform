using System;
using System.Collections;
using System.Collections.Generic;
using HotChocolate.Types;
using HotChocolate.Utilities;

namespace HotChocolate.Execution
{
    internal static class ValueCompletion
    {
        public static void CompleteValue(
            ICompleteValueContext context,
            IType type,
            object result)
        {
            if (type.IsNonNullType())
            {
                CompleteValue(context, type.InnerType(), result);
                HandleNonNullViolation(context);
            }
            else if (result is null)
            {
                context.Value = null;
            }
            else if (IsError(result))
            {
                HandleErrors(context, result);
            }
            else if (type.IsListType())
            {
                CompleteList(context, type.ElementType(), result);
            }
            else if (type is ILeafType leafType)
            {
                CompleteLeafType(context, leafType, result);
            }
            else if (type.IsCompositeType())
            {
                CompleteCompositeType(context, type);
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        private static void CompleteList(
            ICompleteValueContext context,
            IType elementType,
            object result)
        {
            if (result is IEnumerable collection)
            {
                int i = 0;
                var list = new List<object>();
                Path path = context.Path;

                foreach (object element in collection)
                {
                    context.Value = null;
                    context.Path = path.Append(i++);

                    CompleteValue(context, elementType, element);

                    if (context.IsViolatingNonNullType)
                    {
                        context.IsViolatingNonNullType = false;
                        context.Value = null;
                        context.Path = path;
                        return;
                    }

                    list.Add(context.Value);
                }

                context.Value = list;
            }
            else
            {
                // todo : resources
                context.AddError(b =>
                    b.SetMessage("A list values must implement " +
                        $"`{typeof(IEnumerable).FullName}` in order " +
                        "to be completed."));
                context.Value = null;
            }
        }

        private static void CompleteLeafType(
            ICompleteValueContext context,
            ILeafType leafType,
            object result)
        {
            try
            {
                if (TryConvertLeafValue(
                    context.Converter, leafType,
                    result, out object converted))
                {
                    context.Value = leafType.Serialize(converted);
                }
                else
                {
                    // TODO : resources
                    context.AddError(b =>
                        b.SetMessage(
                            "The internal resolver value could not be " +
                            "converted to a valid value of " +
                            $"`{leafType.TypeName()}`."));
                }
            }
            catch (ScalarSerializationException ex)
            {
                // TODO : resources
                context.AddError(b =>
                    b.SetMessage(ex.Message)
                        .SetException(ex));
            }
            catch (Exception ex)
            {
                // TODO : resources
                context.AddError(b =>
                    b.SetMessage("Undefined scalar field serialization error.")
                        .SetException(ex));
            }
        }

        private static void CompleteCompositeType(
            ICompleteValueContext context,
            IType type)
        {
            ObjectType objectType = context.ResolveObjectType(type);

            if (objectType == null)
            {
                // TODO : resources
                context.AddError(b =>
                    b.SetMessage(
                        "Could not resolve the schema type from " +
                        $"`{context.Value.GetType().GetTypeName()}`."));
                context.Value = null;
            }
            else
            {
                var objectResult = new OrderedDictionary();
                context.Value = objectResult;
                context.EnqueueForProcessing(objectType, objectResult);
            }
        }

        private static bool TryConvertLeafValue(
            ITypeConversion converter,
            ILeafType leafType,
            object value,
            out object scalarValue)
        {
            try
            {
                if (value is null)
                {
                    scalarValue = value;
                    return true;
                }

                if (!leafType.ClrType.IsInstanceOfType(value))
                {
                    return converter.TryConvert(
                        typeof(object),
                        leafType.ClrType,
                        value,
                        out scalarValue);
                }

                scalarValue = value;
                return true;
            }
            catch
            {
                scalarValue = null;
                return false;
            }
        }

        private static void HandleNonNullViolation(
            ICompleteValueContext context)
        {
            if (context.Value is null)
            {
                if (!context.HasErrors)
                {
                    // TODO . resources
                    context.AddError(b =>
                        b.SetMessage(
                            "Cannot return null for non-nullable field."));
                }

                context.IsViolatingNonNullType = true;
            }
        }

        private static void HandleErrors(
            ICompleteValueContext context,
            object result)
        {
            if (result is IError error)
            {
                context.AddError(error);
                context.Value = null;
            }
            else if (result is IEnumerable<IError> errors)
            {
                foreach (IError err in errors)
                {
                    context.AddError(err);
                }
                context.Value = null;
            }
        }

        private static bool IsError(object result)
        {
            return result is IError || result is IEnumerable<IError>;
        }
    }
}
