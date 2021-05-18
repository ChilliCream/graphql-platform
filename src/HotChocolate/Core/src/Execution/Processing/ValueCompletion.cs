using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Utilities;
using static HotChocolate.Execution.ErrorHelper;
using static HotChocolate.Execution.Processing.Tasks.ResolverTaskFactory;

namespace HotChocolate.Execution.Processing
{
    internal static partial class ValueCompletion
    {
        public static bool TryComplete(
            IOperationContext operationContext,
            MiddlewareContext resolverContext,
            ISelection selection,
            Path path ,
            IType fieldType ,
            string responseName,
            int responseIndex,
            object? result,
            out object? completedResult)
        {
            TypeKind typeKind = fieldType.Kind;

            if (typeKind is TypeKind.NonNull)
            {
                return TryComplete(
                    operationContext,
                    resolverContext,
                    selection,
                    path,
                    fieldType.InnerType(),
                    responseName,
                    responseIndex,
                    result,
                    out completedResult) &&
                    completedResult is not null;
            }

            if (result is null)
            {
                completedResult = null;
                return true;
            }

            if (typeKind is TypeKind.List)
            {
                return TryCompleteListValue(
                    operationContext,
                    resolverContext,
                    selection,
                    path,
                    fieldType,
                    responseName,
                    responseIndex,
                    result,
                    out completedResult);
            }

            if (typeKind is TypeKind.Scalar or TypeKind.Enum)
            {
                return TryCompleteLeafValue(
                    operationContext,
                    resolverContext,
                    selection,
                    path,
                    fieldType,
                    result,
                    out completedResult);
            }

            if (typeKind is TypeKind.Object or TypeKind.Interface or TypeKind.Union)
            {
                return TryCompleteCompositeValue(
                    operationContext,
                    resolverContext,
                    selection,
                    path,
                    fieldType,
                    result,
                    out completedResult);
            }

            ReportError(
                operationContext,
                resolverContext,
                selection,
                UnexpectedValueCompletionError(selection.SyntaxNode, path));

            completedResult = null;
            return false;
        }

    }


    // leaf
    internal static partial class ValueCompletion
    {
        private static bool TryCompleteLeafValue(
            IOperationContext operationContext,
            MiddlewareContext resolverContext,
            ISelection selection,
            Path path ,
            IType fieldType ,
            object? result,
            out object? completedResult)
        {
            try
            {
                var leafType = (ILeafType)fieldType;
                Type runtimeType = leafType.RuntimeType;

                if (!runtimeType.IsInstanceOfType(result) &&
                    operationContext.Converter.TryConvert(runtimeType, result, out var c))
                {
                    result = c;
                }

                completedResult = leafType.Serialize(result);
                return true;
            }
            catch (SerializationException ex)
            {
                ReportError(
                    operationContext,
                    resolverContext,
                    selection,
                    InvalidLeafValue(ex, selection.SyntaxNode, path));
            }
            catch (Exception ex)
            {
                ReportError(
                    operationContext,
                    resolverContext,
                    selection,
                    UnexpectedLeafValueSerializationError(
                        ex,
                        operationContext.ErrorHandler,
                        selection.SyntaxNode,
                        path));
            }

            completedResult = null;
            return true;
        }
    }

    // composite value
    internal static partial class ValueCompletion
    {
        private static bool TryCompleteCompositeValue(
            IOperationContext operationContext,
            MiddlewareContext resolverContext,
            ISelection selection,
            Path path ,
            IType fieldType,
            object result,
            [NotNullWhen(true)] out object? completedResult)
        {
            if (TryResolveObjectType(
                operationContext,
                resolverContext,
                selection,
                path,
                fieldType,
                result,
                out ObjectType? objectType))
            {
                SelectionSetNode selectionSet = selection.SyntaxNode.SelectionSet!;
                ISelectionSet selections = operationContext.CollectFields(selectionSet, objectType);
                Type runtimeType = objectType.RuntimeType;

                if (!runtimeType.IsInstanceOfType(result) &&
                    operationContext.Converter.TryConvert(runtimeType, result, out var converted))
                {
                    result = converted;
                }

                completedResult = EnqueueOrInlineResolverTasks(
                    operationContext,
                    resolverContext,
                    path,
                    result,
                    selections);
                return true;
            }

            ReportError(
                operationContext,
                resolverContext,
                selection,
                ValueCompletion_CouldNotResolveAbstractType(selection.SyntaxNode, path, result));

            completedResult = null;
            return false;
        }

        private static bool TryResolveObjectType(
            IOperationContext operationContext,
            MiddlewareContext resolverContext,
            ISelection selection,
            Path path ,
            IType fieldType,
            object result,
            [NotNullWhen(true)] out ObjectType? objectType)
        {
            try
            {
                if (resolverContext.ValueType is ObjectType valueType &&
                    ReferenceEquals(selection, resolverContext.Selection))
                {
                    objectType = valueType;
                    return true;
                }

                switch (fieldType)
                {
                    case ObjectType ot:
                        objectType = ot;
                        return true;

                    case InterfaceType it:
                        objectType = it.ResolveConcreteType(resolverContext, result);
                        return objectType is not null;

                    case UnionType ut:
                        objectType = ut.ResolveConcreteType(resolverContext, result);
                        return objectType is not null;
                }

                ReportError(
                    operationContext,
                    resolverContext,
                    selection,
                    UnableToResolveTheAbstractType(fieldType.Print(), selection.SyntaxNode, path));
            }
            catch (Exception ex)
            {
                ReportError(
                    operationContext,
                    resolverContext,
                    selection,
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

    // lists
    internal static partial class ValueCompletion
    {
        private static bool TryCompleteListValue(
            IOperationContext operationContext,
            MiddlewareContext resolverContext,
            ISelection selection,
            Path path ,
            IType fieldType ,
            string responseName,
            int responseIndex,
            object? result,
            out object? completedValue)
        {
            IType elementType = fieldType.ElementType();

            if (elementType.Kind is TypeKind.Object or TypeKind.Interface or TypeKind.Union)
            {
                return TryCompleteCompositeListValue(
                    operationContext,
                    resolverContext,
                    selection,
                    path,
                    fieldType,
                    responseName,
                    responseIndex,
                    result,
                    out completedValue);
            }

            return TryCompleteOtherListValue(
                operationContext,
                resolverContext,
                selection,
                path,
                fieldType,
                responseName,
                responseIndex,
                result,
                out completedValue);
        }

        private static bool TryCompleteCompositeListValue(
            IOperationContext operationContext,
            MiddlewareContext resolverContext,
            ISelection selection,
            Path path ,
            IType fieldType ,
            string responseName,
            int responseIndex,
            object? result,
            out object? completedResult)
        {
            ResultMapList resultList = operationContext.Result.RentResultMapList();
            IType elementType = fieldType.InnerType();
            resultList.IsNullable = elementType.Kind == TypeKind.NonNull;

            if (result is Array array)
            {
                for (var i = 0; i < array.Length; i++)
                {
                    if (!TryCompleteElement(path.Append(i), array.GetValue(i)))
                    {
                        completedResult = null;
                        return true;
                    }
                }

                completedResult = resultList;
                return true;
            }

            if (result is IList list)
            {
                for (var i = 0; i < list.Count; i++)
                {
                    if (!TryCompleteElement(path.Append(i), list[i]))
                    {
                        completedResult = null;
                        return true;
                    }
                }

                completedResult = resultList;
                return true;
            }

            if (result is IEnumerable enumerable)
            {
                var index = 0;

                foreach (var element in enumerable)
                {
                    if (!TryCompleteElement(path.Append(index++), element))
                    {
                        completedResult = null;
                        return true;
                    }
                }

                completedResult = resultList;
                return true;
            }

            ReportError(
                operationContext,
                resolverContext,
                selection,
                ListValueIsNotSupported(resultList.GetType(), selection.SyntaxNode, path));

            completedResult = null;
            return false;

            bool TryCompleteElement(Path elementPath, object? elementResult)
            {
                if (TryComplete(
                    operationContext,
                    resolverContext,
                    selection,
                    elementPath,
                    elementType,
                    responseName,
                    responseIndex,
                    elementResult,
                    out var completedElement) &&
                    completedElement is ResultMap resultMap)
                {
                    resultMap.Parent = resultList;
                    resultList.Add(resultMap);
                }
                else if (resultList.IsNullable)
                {
                    resultList.Add(null);
                }
                else
                {
                    return false;
                }

                return true;
            }
        }

        private static bool TryCompleteOtherListValue(
            IOperationContext operationContext,
            MiddlewareContext resolverContext,
            ISelection selection,
            Path path ,
            IType fieldType ,
            string responseName,
            int responseIndex,
            object? result,
            out object? completedResult)
        {
            ResultList resultList = operationContext.Result.RentResultList();
            IType elementType = fieldType.InnerType();
            resultList.IsNullable = elementType.Kind == TypeKind.NonNull;
            var isElementList = elementType.IsListType();

            if (result is Array array)
            {
                for (var i = 0; i < array.Length; i++)
                {
                    if (!TryCompleteElement(path.Append(i), array.GetValue(i)))
                    {
                        completedResult = null;
                        return true;
                    }
                }

                completedResult = resultList;
                return true;
            }

            if (result is IList list)
            {
                for (var i = 0; i < list.Count; i++)
                {
                    if (!TryCompleteElement(path.Append(i),  list[i]))
                    {
                        completedResult = null;
                        return true;
                    }
                }

                completedResult = resultList;
                return true;
            }

            if (result is IEnumerable enumerable)
            {
                var index = 0;

                foreach (var element in enumerable)
                {
                    if (!TryCompleteElement(path.Append(index++),  element))
                    {
                        completedResult = null;
                        return true;
                    }
                }

                completedResult = resultList;
                return true;
            }

            ReportError(
                operationContext,
                resolverContext,
                selection,
                ListValueIsNotSupported(resultList.GetType(), selection.SyntaxNode, path));

            completedResult = null;
            return false;

            bool TryCompleteElement(Path elementPath, object? elementResult)
            {
                if (TryComplete(
                        operationContext,
                        resolverContext,
                        selection,
                        elementPath,
                        elementType,
                        responseName,
                        responseIndex,
                        elementResult,
                        out var completedElement) &&
                    completedElement is not null)
                {
                    resultList.Add(completedElement);

                    if (isElementList)
                    {
                        ((IHasResultDataParent)completedElement).Parent = resultList;
                    }
                }
                else if (resultList.IsNullable)
                {
                    resultList.Add(null);
                }
                else
                {
                    return false;
                }

                return true;
            }
        }
    }

    // tools
    internal static partial class ValueCompletion
    {
        public static  void ReportError(
            IOperationContext operationContext,
            MiddlewareContext resolverContext,
            ISelection selection,
            IError error)
        {
            error = operationContext.ErrorHandler.Handle(error);
            operationContext.Result.AddError(error, selection.SyntaxNode);
            operationContext.DiagnosticEvents.ResolverError(resolverContext, error);
        }

        public static void ReportError(
            IOperationContext operationContext,
            MiddlewareContext resolverContext,
            ISelection selection,
            Path path,
            Exception exception)
        {
            if (exception is null)
            {
                throw new ArgumentNullException(nameof(exception));
            }

            if (exception is GraphQLException graphQLException)
            {
                foreach (IError error in graphQLException.Errors)
                {
                    ReportError(operationContext, resolverContext, selection, error);
                }
            }
            else
            {
                IError error = operationContext.ErrorHandler
                    .CreateUnexpectedError(exception)
                    .SetPath(path)
                    .AddLocation(selection.SyntaxNode)
                    .Build();

                ReportError(operationContext, resolverContext, selection, error);
            }
        }
    }
}
