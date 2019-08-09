using System.Threading;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using HotChocolate.Properties;
using HotChocolate.Types;
using HotChocolate.Utilities;

namespace HotChocolate.Execution
{
    internal static class ValueCompletion
    {
        private static ThreadLocal<CompleteValueContext> _completionContext =
            new ThreadLocal<CompleteValueContext>(
                () => new CompleteValueContext());

        public static void CompleteValue(
            Action<ResolverContext> enqueueNext,
            ResolverContext resolverContext)
        {
            CompleteValueContext completionContext = _completionContext.Value;
            completionContext.Clear();

            completionContext.EnqueueNext = enqueueNext;
            completionContext.ResolverContext = resolverContext;

            CompleteValue(
                completionContext,
                resolverContext.Field.Type,
                resolverContext.Result);

            if (completionContext.IsViolatingNonNullType)
            {
                resolverContext.PropagateNonNullViolation.Invoke();
            }
            else
            {
                resolverContext.SetCompletedValue(completionContext.Value);
            }
        }

        private static void CompleteValue(
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
                CompleteCompositeType(context, type, result);
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
                var i = 0;
                var list = new List<object>();
                Path path = context.Path;

                foreach (var element in collection)
                {
                    list.Add(null);

                    var local = i;
                    context.Value = null;
                    context.Path = path.Append(i);
                    context.SetElementNull = () => list[local] = null;

                    CompleteValue(context, elementType, element);

                    if (context.IsViolatingNonNullType)
                    {
                        context.IsViolatingNonNullType = false;
                        context.Value = null;
                        context.Path = path;
                        return;
                    }

                    list[i] = context.Value;
                    i++;
                }

                context.IsViolatingNonNullType = false;
                context.Path = path;
                context.Value = list;
            }
            else
            {
                context.AddError(b =>
                    b.SetMessage(string.Format(
                        CultureInfo.InvariantCulture,
                        CoreResources.CompleteList_ListTypeInvalid,
                        typeof(IEnumerable).FullName)));
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
                    context.AddError(b =>
                        b.SetMessage(string.Format(
                            CultureInfo.InvariantCulture,
                            CoreResources.CompleteLeafType_CannotConvertValue,
                            leafType.TypeName())));
                }
            }
            catch (ScalarSerializationException ex)
            {
                context.AddError(b =>
                    b.SetMessage(ex.Message)
                        .SetException(ex));
            }
            catch (Exception ex)
            {
                context.AddError(b =>
                    b.SetMessage(CoreResources
                        .CompleteLeadType_UndefinedError)
                        .SetException(ex));
            }
        }

        private static void CompleteCompositeType(
            ICompleteValueContext context,
            IType type,
            object result)
        {
            ObjectType objectType = context.ResolveObjectType(type, result);

            if (objectType == null)
            {
                context.AddError(b =>
                    b.SetMessage(string.Format(
                        CultureInfo.InvariantCulture,
                        CoreResources.CompleteCompositeType_UnknownSchemaType,
                        result.GetType().GetTypeName(),
                        type.NamedType().Name.Value)));
                context.Value = null;
            }
            else
            {
                var objectResult = new OrderedDictionary();
                context.Value = objectResult;
                context.EnqueueForProcessing(objectType, objectResult, result);
            }
        }

        private static bool TryConvertLeafValue(
            ITypeConversion converter,
            IHasClrType leafType,
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
                    context.AddError(b => b
                        .SetMessage(CoreResources
                            .HandleNonNullViolation_Message)
                        .SetCode(ExecErrorCodes.NonNullViolation));
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
