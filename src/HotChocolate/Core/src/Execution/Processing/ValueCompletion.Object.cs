using System.Diagnostics.CodeAnalysis;
using HotChocolate.Types;
using HotChocolate.Utilities;
using static HotChocolate.Execution.ErrorHelper;
using static HotChocolate.Execution.Processing.PathHelper;
using static HotChocolate.Execution.Processing.Tasks.ResolverTaskFactory;

namespace HotChocolate.Execution.Processing;

internal static partial class ValueCompletion
{
    private static ObjectResult? CompleteCompositeValue(
        ValueCompletionContext context,
        ISelection selection,
        IType type,
        ResultData parent,
        int index,
        object result)
    {
        var operationContext = context.OperationContext;

        if (TryResolveObjectType(context, selection, type, parent, index, result, out var objectType))
        {
            var selectionSet = operationContext.CollectFields(selection, objectType);
            var runtimeType = objectType.RuntimeType;

            if (!runtimeType.IsInstanceOfType(result) &&
                operationContext.Converter.TryConvert(runtimeType, result, out var converted))
            {
                result = converted;
            }

            return EnqueueOrInlineResolverTasks(context, objectType, parent, index, result, selectionSet);
        }

        var errorPath = CreatePathFromContext(selection, parent, index);
        var error = ValueCompletion_CouldNotResolveAbstractType(selection.SyntaxNode, errorPath, result);
        operationContext.ReportError(error, context.ResolverContext, selection);
        return null;
    }

    private static bool TryResolveObjectType(
        ValueCompletionContext context,
        ISelection selection,
        IType fieldType,
        ResultData parent,
        int index,
        object result,
        [NotNullWhen(true)] out ObjectType? objectType)
    {
        try
        {
            if (context.ResolverContext.ValueType is ObjectType valueType &&
                ReferenceEquals(selection, context.ResolverContext.Selection))
            {
                objectType = valueType;
                return true;
            }

            switch (fieldType.Kind)
            {
                case TypeKind.Object:
                    objectType = (ObjectType) fieldType;
                    return true;

                case TypeKind.Interface:
                    objectType = ((InterfaceType) fieldType)
                        .ResolveConcreteType(context.ResolverContext, result);
                    return objectType is not null;

                case TypeKind.Union:
                    objectType = ((UnionType) fieldType)
                        .ResolveConcreteType(context.ResolverContext, result);
                    return objectType is not null;
            }

            var error = UnableToResolveTheAbstractType(
                fieldType.Print(),
                selection.SyntaxNode,
                CreatePathFromContext(selection, parent, index));
            context.OperationContext.ReportError(error, context.ResolverContext, selection);
        }
        catch (Exception ex)
        {
            var error = UnexpectedErrorWhileResolvingAbstractType(
                ex,
                fieldType.Print(),
                selection.SyntaxNode,
                CreatePathFromContext(selection, parent, index));
            context.OperationContext.ReportError(error, context.ResolverContext, selection);
        }

        objectType = null;
        return false;
    }
}
