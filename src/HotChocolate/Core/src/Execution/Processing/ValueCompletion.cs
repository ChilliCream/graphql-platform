using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Utilities;
using static HotChocolate.Execution.ErrorHelper;

namespace HotChocolate.Execution.Processing
{
    internal static partial class ValueCompletion
    {
        public static bool TryComplete(
            IOperationContext operationContext,
            IMiddlewareContext middlewareContext,
            Path path,
            IType fieldType,
            object? result,
            out object? completedResult) =>
            TryComplete(
                operationContext,
                middlewareContext,
                (ISelection)middlewareContext.Selection,
                path,
                fieldType,
                result,
                out completedResult);

        public static bool TryComplete(
            IOperationContext operationContext,
            IMiddlewareContext middlewareContext,
            ISelection selection,
            Path path,
            IType fieldType,
            object? result,
            out object? completedResult)
        {
            if (fieldType.IsNonNullType())
            {
                if (TryComplete(
                    operationContext,
                    middlewareContext,
                    selection,
                    path,
                    fieldType.InnerType(),
                    result,
                    out completedResult) &&
                    completedResult is not null)
                {
                    return true;
                }

                return false;
            }

            if (result is null)
            {
                completedResult = null;
                return true;
            }

            if (fieldType.IsListType())
            {
                if (TryCompleteListValue(
                    operationContext,
                    middlewareContext,
                    selection,
                    path,
                    fieldType,
                    result,
                    out IResultData? completedList))
                {
                    completedResult = completedList;
                    return true;
                }

                completedResult = null;
                return false;
            }

            if (fieldType is ILeafType leafType)
            {
                return TryCompleteLeafValue(
                    operationContext,
                    middlewareContext,
                    selection,
                    path,
                    leafType,
                    result,
                    out completedResult);
            }

            if (fieldType.IsCompositeType())
            {
                if (TryCompleteCompositeValue(
                    operationContext,
                    middlewareContext,
                    selection,
                    path,
                    fieldType,
                    result,
                    out ResultMap? completedResultMap))
                {
                    completedResult = completedResultMap;
                    return true;
                }

                completedResult = null;
                return false;
            }

            middlewareContext.ReportError(
                UnexpectedValueCompletionError(
                    middlewareContext.Selection.SyntaxNode,
                    path));

            completedResult = null;
            return false;
        }

        private static void SetParent(object value, IResultData parentList)
        {
            if (value is IHasResultDataParent result)
            {
                result.Parent = parentList;
            }
        }

        private static bool TryCompleteLeafValue(
            IOperationContext operationContext,
            IMiddlewareContext middlewareContext,
            ISelection selection,
            Path path,
            ILeafType fieldType,
            object result,
            out object? completedResult)
        {
            try
            {
                if (!fieldType.RuntimeType.IsInstanceOfType(result) &&
                    operationContext.Converter.TryConvert(fieldType.RuntimeType, result, out var c))
                {
                    result = c;
                }
                completedResult = fieldType.Serialize(result);
                return true;
            }
            catch (SerializationException ex)
            {
                middlewareContext.ReportError(
                    InvalidLeafValue(
                        ex,
                        selection.SyntaxNode,
                        path));
            }
            catch (Exception ex)
            {
                middlewareContext.ReportError(
                    UnexpectedLeafValueSerializationError(
                        ex,
                        operationContext.ErrorHandler,
                        selection.SyntaxNode,
                        path));
            }

            completedResult = null;
            return true;
        }

        private static bool TryCompleteCompositeValue(
            IOperationContext operationContext,
            IMiddlewareContext middlewareContext,
            ISelection selection,
            Path path,
            IType fieldType,
            object result,
            [NotNullWhen(true)] out ResultMap? completedResult)
        {
            if (TryResolveObjectType(
                middlewareContext, selection, path, fieldType, result,
                out ObjectType? objectType))
            {
                SelectionSetNode selectionSet = selection.SyntaxNode.SelectionSet!;
                ISelectionSet selections = operationContext.CollectFields(selectionSet, objectType);

                completedResult = selections.EnqueueResolverTasks(
                    operationContext,
                    middlewareContext,
                    path,
                    result,
                    ReferenceEquals(middlewareContext.Selection, selection));
                return true;
            }

            middlewareContext.ReportError(
                ValueCompletion_CouldNotResolveAbstractType(
                    selection.SyntaxNode,
                    path,
                    result));
            completedResult = null;
            return false;
        }

        private static bool TryResolveObjectType(
            IMiddlewareContext middlewareContext,
            ISelection selection,
            Path path,
            IType fieldType,
            object result,
            [NotNullWhen(true)] out ObjectType? objectType)
        {
            try
            {
                if (ReferenceEquals(middlewareContext.Selection, selection) &&
                    middlewareContext.ValueType is ObjectType vot)
                {
                    objectType = vot;
                    return true;
                }

                if (fieldType is ObjectType ot)
                {
                    objectType = ot;
                    return true;
                }

                if (fieldType is InterfaceType it)
                {
                    objectType = it.ResolveConcreteType(middlewareContext, result);
                    return objectType is { };
                }

                if (fieldType is UnionType ut)
                {
                    objectType = ut.ResolveConcreteType(middlewareContext, result);
                    return objectType is { };
                }

                middlewareContext.ReportError(
                    UnableToResolveTheAbstractType(
                        fieldType.Print(),
                        selection.SyntaxNode,
                        path));
            }
            catch (Exception ex)
            {
                middlewareContext.ReportError(
                    UnexpectedErrorWhileResolvingAbstractType(
                        ex,
                        fieldType.Print(),
                        selection.SyntaxNode,
                        path));
            }

            objectType = null;
            return false;
        }
    }
}
