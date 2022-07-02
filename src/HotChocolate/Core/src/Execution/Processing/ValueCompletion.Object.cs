using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Execution.Processing.Tasks;
using HotChocolate.Types;
using HotChocolate.Utilities;
using static HotChocolate.Execution.ErrorHelper;
using static HotChocolate.Execution.Processing.Tasks.ResolverTaskFactory;

namespace HotChocolate.Execution.Processing;

internal static partial class ValueCompletion
{
    private static ObjectResult? CompleteCompositeValue(
        IOperationContext operationContext,
        MiddlewareContext resolverContext,
        List<ResolverTask> tasks,
        ISelection selection,
        Path path,
        IType fieldType,
        object result)
    {
        if (TryResolveObjectType(
            operationContext,
            resolverContext,
            selection,
            path,
            fieldType,
            result,
            out var objectType))
        {
            var selectionSet = operationContext.CollectFields(selection, objectType);
            var runtimeType = objectType.RuntimeType;

            if (!runtimeType.IsInstanceOfType(result) &&
                operationContext.Converter.TryConvert(runtimeType, result, out var converted))
            {
                result = converted;
            }

            return EnqueueOrInlineResolverTasks(
                operationContext,
                resolverContext,
                path,
                objectType,
                result,
                selectionSet,
                tasks);
        }

        ReportError(
            operationContext,
            resolverContext,
            selection,
            ValueCompletion_CouldNotResolveAbstractType(selection.SyntaxNode, path, result));

        return null;
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

            switch (fieldType.Kind)
            {
                case TypeKind.Object:
                    objectType = (ObjectType)fieldType;
                    return true;

                case TypeKind.Interface:
                    objectType = ((InterfaceType)fieldType)
                        .ResolveConcreteType(resolverContext, result);
                    return objectType is not null;

                case TypeKind.Union:
                    objectType = ((UnionType)fieldType)
                        .ResolveConcreteType(resolverContext, result);
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
