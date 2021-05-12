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
    internal ref struct ValueCompletionContext
    {
        public ValueCompletionContext(
            IOperationContext operationContext,
            MiddlewareContext resolverContext)
            : this(
                operationContext,
                resolverContext,
                (ISelection)resolverContext.Selection,
                resolverContext.Path,
                resolverContext.Field.Type,
                resolverContext.ResponseName,
                resolverContext.ResponseIndex,
                resolverContext.Result)
        {
        }

        public ValueCompletionContext(
            IOperationContext operationContext,
            MiddlewareContext resolverContext,
            ISelection selection,
            Path path,
            IType fieldType,
            string responseName,
            int responseIndex,
            object? result)
        {
            OperationContext = operationContext;
            ResolverContext = resolverContext;
            Selection = selection;
            Path = path;
            FieldType = fieldType;
            ResponseName = responseName;
            ResponseIndex = responseIndex;
            Result = result;
        }

        public IOperationContext OperationContext { get; }
        public MiddlewareContext ResolverContext { get; }
        public ISelection Selection { get; set; }
        public Path Path { get; set; }
        public IType FieldType { get; set; }
        public string ResponseName { get; set; }
        public int ResponseIndex { get; set; }
        public object? Result { get; set; }

        public void ReportError(IError error)
        {
            error = OperationContext.ErrorHandler.Handle(error);
            OperationContext.Result.AddError(error, Selection.SyntaxNode);
            OperationContext.DiagnosticEvents.ResolverError(ResolverContext, error);
        }

        public void ReportError(Exception exception)
        {
            if (exception is null)
            {
                throw new ArgumentNullException(nameof(exception));
            }

            if (exception is GraphQLException graphQLException)
            {
                foreach (IError error in graphQLException.Errors)
                {
                    ReportError(error);
                }
            }
            else
            {
                IError error = OperationContext.ErrorHandler
                    .CreateUnexpectedError(exception)
                    .SetPath(Path)
                    .AddLocation(Selection.SyntaxNode)
                    .Build();

                ReportError(error);
            }
        }

        public ValueCompletionContext WithInnerFieldType()
        {
            return new(
                OperationContext,
                ResolverContext,
                Selection,
                Path,
                FieldType.InnerType(),
                ResponseName,
                ResponseIndex,
                Result);
        }

        public ValueCompletionContext Copy()
        {
            return new(
                OperationContext,
                ResolverContext,
                Selection,
                Path,
                FieldType,
                ResponseName,
                ResponseIndex,
                Result);
        }
    }

    internal static partial class ValueCompletion2
    {
        public static bool TryComplete(
            ref ValueCompletionContext context,
            out object? completedResult)
        {
            TypeKind typeKind = context.FieldType.Kind;

            if (typeKind == TypeKind.NonNull)
            {
                ValueCompletionContext nullableContext = context.WithInnerFieldType();
                return TryComplete(ref nullableContext, out completedResult) &&
                    completedResult is not null;
            }

            if (context.Result is null)
            {
                completedResult = null;
                return true;
            }

            if (typeKind == TypeKind.List)
            {
                return TryCompleteListValue(ref context, out completedResult);
            }

            if (typeKind == TypeKind.Scalar || typeKind == TypeKind.Enum)
            {
                return TryCompleteLeafValue(ref context, out completedResult);
            }

            if (typeKind == TypeKind.Object ||
                typeKind == TypeKind.Interface ||
                typeKind == TypeKind.Union)
            {
                return TryCompleteCompositeValue(ref context, out completedResult);
            }

            context.ReportError(
                UnexpectedValueCompletionError(
                    context.Selection.SyntaxNode,
                    context.Path));

            completedResult = null;
            return false;
        }

    }


    // leaf
    internal static partial class ValueCompletion2
    {
        private static bool TryCompleteLeafValue(
            ref ValueCompletionContext context,
            out object? completedResult)
        {
            try
            {
                var leafType = (ILeafType)context.FieldType;
                Type runtimeType = leafType.RuntimeType;
                object result = context.Result!;

                if (!runtimeType.IsInstanceOfType(result) &&
                    context.OperationContext.Converter.TryConvert(runtimeType, result, out var c))
                {
                    result = c;
                }

                completedResult = leafType.Serialize(result);
                return true;
            }
            catch (SerializationException ex)
            {
                context.ReportError(
                    InvalidLeafValue(
                        ex,
                        context.Selection.SyntaxNode,
                        context.Path));
            }
            catch (Exception ex)
            {
                context.ReportError(
                    UnexpectedLeafValueSerializationError(
                        ex,
                        context.OperationContext.ErrorHandler,
                        context.Selection.SyntaxNode,
                        context.Path));
            }

            completedResult = null;
            return true;
        }
    }

    // composite value
    internal static partial class ValueCompletion2
    {
        private static bool TryCompleteCompositeValue(
            ref ValueCompletionContext context,
            [NotNullWhen(true)] out object? completedResult)
        {
            if (TryResolveObjectType(ref context, out ObjectType? objectType))
            {
                SelectionSetNode selectionSet = context.Selection.SyntaxNode.SelectionSet!;
                ISelectionSet selections = context.OperationContext.CollectFields(selectionSet, objectType);
                Type runtimeType = objectType.RuntimeType;

                if (!runtimeType.IsInstanceOfType(context.Result) &&
                    context.OperationContext.Converter.TryConvert(runtimeType, context.Result, out var converted))
                {
                    context.Result = converted;
                }

                completedResult = EnqueueOrInlineResolverTasks(ref context, selections);
                return true;
            }

            context.ReportError(
                ValueCompletion_CouldNotResolveAbstractType(
                    context.Selection.SyntaxNode,
                    context.Path,
                    context.Result!));
            completedResult = null;
            return false;
        }

        private static bool TryResolveObjectType(
            ref ValueCompletionContext context,
            [NotNullWhen(true)] out ObjectType? objectType)
        {
            try
            {
                if (context.ResolverContext.ValueType is ObjectType valueType &&
                    ReferenceEquals(context.Selection, context.ResolverContext.Selection))
                {
                    objectType = valueType;
                    return true;
                }

                switch (context.FieldType)
                {
                    case ObjectType ot:
                        objectType = ot;
                        return true;

                    case InterfaceType it:
                        objectType = it.ResolveConcreteType(context.ResolverContext, context.Result!);
                        return objectType is not null;

                    case UnionType ut:
                        objectType = ut.ResolveConcreteType(context.ResolverContext, context.Result!);
                        return objectType is not null;
                }

                context.ReportError(
                    UnableToResolveTheAbstractType(
                        context.FieldType.Print(),
                        context.Selection.SyntaxNode,
                        context.Path));
            }
            catch (Exception ex)
            {
                context.ReportError(
                    UnexpectedErrorWhileResolvingAbstractType(
                        ex,
                        context.FieldType.Print(),
                        context.Selection.SyntaxNode,
                        context.Path));
            }

            objectType = null;
            return false;
        }
    }

    // lists
    internal static partial class ValueCompletion2
    {
        private static bool TryCompleteListValue(
            ref ValueCompletionContext context,
            out object? completedValue)
        {
            IType elementType = context.FieldType.InnerType();

            if (elementType.Kind == TypeKind.Object ||
                elementType.Kind == TypeKind.Interface ||
                elementType.Kind == TypeKind.Union)
            {
                return TryCompleteCompositeListValue(ref context, out completedValue);
            }

            return TryCompleteOtherListValue(ref context, out completedValue);
        }

        private static bool TryCompleteCompositeListValue(
            ref ValueCompletionContext context,
            out object? completedResult)
        {
            ValueCompletionContext elementContext = context.WithInnerFieldType();
            Path path = context.Path;

            ResultMapList resultList = context.OperationContext.Result.RentResultMapList();
            resultList.IsNullable = elementContext.FieldType.Kind == TypeKind.NonNull;

            if (context.Result is Array array)
            {
                for (var i = 0; i < array.Length; i++)
                {
                    elementContext.Path = path.Append(i);
                    elementContext.Result = array.GetValue(i);

                    if (!TryCompleteElement(ref elementContext))
                    {
                        completedResult = null;
                        return true;
                    }
                }

                completedResult = resultList;
                return true;
            }

            if (context.Result is IList list)
            {
                for (var i = 0; i < list.Count; i++)
                {
                    elementContext.Path = path.Append(i);
                    elementContext.Result = list[i];

                    if (!TryCompleteElement(ref elementContext))
                    {
                        completedResult = null;
                        return true;
                    }
                }

                completedResult = resultList;
                return true;
            }

            if (context.Result is IEnumerable enumerable)
            {
                var index = 0;

                foreach (var element in enumerable)
                {
                    elementContext.Path = path.Append(index++);
                    elementContext.Result = element;

                    if (!TryCompleteElement(ref elementContext))
                    {
                        completedResult = null;
                        return true;
                    }
                }

                completedResult = resultList;
                return true;
            }

            context.ReportError(
                ListValueIsNotSupported(
                    resultList.GetType(),
                    context.Selection.SyntaxNode,
                    path));
            completedResult = null;
            return false;

            bool TryCompleteElement(ref ValueCompletionContext elementContext)
            {
                if (TryComplete(ref elementContext, out var completedElement) &&
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
            ref ValueCompletionContext context,
            out object? completedResult)
        {
            ValueCompletionContext elementContext = context.WithInnerFieldType();
            Path path = context.Path;

            ResultList resultList = context.OperationContext.Result.RentResultList();
            resultList.IsNullable = elementContext.FieldType.Kind == TypeKind.NonNull;
            var isElementList = elementContext.FieldType.IsListType();

            if (context.Result is Array array)
            {
                for (var i = 0; i < array.Length; i++)
                {
                    elementContext.Path = path.Append(i);
                    elementContext.Result = array.GetValue(i);

                    if (!TryCompleteElement(ref elementContext))
                    {
                        completedResult = null;
                        return true;
                    }
                }

                completedResult = resultList;
                return true;
            }

            if (context.Result is IList list)
            {
                for (var i = 0; i < list.Count; i++)
                {
                    elementContext.Path = path.Append(i);
                    elementContext.Result = list[i];

                    if (!TryCompleteElement(ref elementContext))
                    {
                        completedResult = null;
                        return true;
                    }
                }

                completedResult = resultList;
                return true;
            }

            if (context.Result is IEnumerable enumerable)
            {
                var index = 0;

                foreach (var element in enumerable)
                {
                    elementContext.Path = path.Append(index++);
                    elementContext.Result = element;

                    if (!TryCompleteElement(ref elementContext))
                    {
                        completedResult = null;
                        return true;
                    }
                }

                completedResult = resultList;
                return true;
            }

            context.ReportError(
                ListValueIsNotSupported(
                    resultList.GetType(),
                    context.Selection.SyntaxNode,
                    path));
            completedResult = null;
            return false;

            bool TryCompleteElement(ref ValueCompletionContext elementContext)
            {
                if (TryComplete(ref elementContext, out var completedElementValue) &&
                    completedElementValue is not null)
                {
                    resultList.Add(completedElementValue);

                    if (isElementList)
                    {
                        SetParent(completedElementValue, resultList);
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

            void SetParent(object elementValue, IResultData list) =>
                ((IHasResultDataParent)elementValue).Parent = list;
        }
    }

    // tools
    internal static partial class ValueCompletion2
    {
        public static void SetCompletedValue(
            this ValueCompletionContext context,
            object? completedValue,
            ResultMap resultMap)
        {
            bool isNonNullType = context.FieldType.IsNullableType();

            if (completedValue is null && isNonNullType)
            {
                // if we detect a non-null violation we will stash it for later.
                // the non-null propagation is delayed so that we can parallelize better.
                context.OperationContext.Result.AddNonNullViolation(
                    context.Selection.SyntaxNode,
                    context.Path,
                    resultMap);
            }
            else
            {
                resultMap.SetValue(
                    context.ResponseIndex,
                    context.ResponseName,
                    completedValue,
                    isNonNullType);
            }
        }
    }
}
