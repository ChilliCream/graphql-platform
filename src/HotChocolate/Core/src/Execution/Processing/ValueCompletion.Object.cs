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
        OperationContext operationContext,
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

            // if (!runtimeType.IsInstanceOfType(result) &&
            //     operationContext.Converter.TryConvert(runtimeType, result, out var converted))
            // {
            //     result = converted;
            // }

            return EnqueueOrInlineResolverTasks(
                operationContext,
                resolverContext,
                path,
                objectType,
                result,
                selectionSet,
                tasks);
        }

        var error = ValueCompletion_CouldNotResolveAbstractType(selection.SyntaxNode, path, result);
        operationContext.ReportError(error, resolverContext, selection);
        return null;
    }

    private static bool TryResolveObjectType(
        OperationContext operationContext,
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

            var error = UnableToResolveTheAbstractType(
                fieldType.Print(),
                selection.SyntaxNode,
                path);
            operationContext.ReportError(error, resolverContext, selection);
        }
        catch (Exception ex)
        {
            var error = UnexpectedErrorWhileResolvingAbstractType(
                ex,
                fieldType.Print(),
                selection.SyntaxNode,
                path);
            operationContext.ReportError(error, resolverContext, selection);
        }

        objectType = null;
        return false;
    }
}
