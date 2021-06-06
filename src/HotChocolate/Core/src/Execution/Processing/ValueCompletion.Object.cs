using System;
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
        private static bool TryCompleteCompositeValue(
            IOperationContext operationContext,
            MiddlewareContext resolverContext,
            ISelection selection,
            Path path,
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
            Path path,
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
}
