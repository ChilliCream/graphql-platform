using System.Diagnostics.CodeAnalysis;
using HotChocolate.Text.Json;
using HotChocolate.Types;
using HotChocolate.Utilities;
using static HotChocolate.Execution.ErrorHelper;
using static HotChocolate.Execution.Processing.Tasks.ResolverTaskFactory;

namespace HotChocolate.Execution.Processing;

internal static partial class ValueCompletion
{
    private static void CompleteCompositeValue(
        ValueCompletionContext context,
        Selection selection,
        IType type,
        ResultElement resultValue,
        object runtimeValue)
    {
        var operationContext = context.OperationContext;

        if (TryResolveObjectType(context, selection, type, resultValue, runtimeValue, out var objectType))
        {
            var selectionSet = selection.DeclaringOperation.GetSelectionSet(selection, objectType);
            var runtimeType = objectType.RuntimeType;

            if (!runtimeType.IsInstanceOfType(runtimeValue)
                && operationContext.Converter.TryConvert(runtimeType, runtimeValue, out var converted))
            {
                runtimeValue = converted;
            }

            EnqueueOrInlineResolverTasks(
                context,
                selectionSet,
                objectType,
                resultValue,
                runtimeValue);
            return;
        }

        var error = ValueCompletion_CouldNotResolveAbstractType(selection, resultValue.Path, runtimeValue);
        operationContext.ReportError(error, context.ResolverContext);
    }

    private static bool TryResolveObjectType(
        ValueCompletionContext context,
        Selection selection,
        IType fieldType,
        ResultElement resultValue,
        object runtimeValue,
        [NotNullWhen(true)] out ObjectType? objectType)
    {
        try
        {
            if (context.ResolverContext.ValueType is ObjectType valueType
                && ReferenceEquals(selection, context.ResolverContext.Selection))
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
                        .ResolveConcreteType(context.ResolverContext, runtimeValue);
                    return objectType is not null;

                case TypeKind.Union:
                    objectType = ((UnionType)fieldType)
                        .ResolveConcreteType(context.ResolverContext, runtimeValue);
                    return objectType is not null;
            }

            var error = UnableToResolveTheAbstractType(
                fieldType.Print(),
                selection,
                resultValue.Path);
            context.OperationContext.ReportError(error, context.ResolverContext);
        }
        catch (Exception ex)
        {
            var error = UnexpectedErrorWhileResolvingAbstractType(
                ex,
                fieldType.Print(),
                selection,
                resultValue.Path);
            context.OperationContext.ReportError(error, context.ResolverContext);
        }

        objectType = null;
        return false;
    }
}
