using System;
using System.Collections.Generic;
using HotChocolate.Internal;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Execution
{
    internal class ObjectFieldValueHandler
        : IFieldValueHandler
    {
        public void CompleteValue(
            IFieldValueCompletionContext context,
            Action<IFieldValueCompletionContext> nextHandler)
        {
            if (context.Type.IsObjectType()
                || context.Type.IsInterfaceType()
                || context.Type.IsUnionType())
            {
                ObjectType objectType = ResolveObjectType(
                    context.ResolverContext, context.Type, context.Value);
                if (objectType == null)
                {
                    context.AddError(new FieldError(
                        "Could not resolve the schema type from " +
                        $"`{context.Value.GetType().GetTypeName()}`.",
                        context.Selection.Node));
                    return;
                }
                CompleteObjectValue(context, nextHandler, objectType);
            }
            else
            {
                nextHandler?.Invoke(context);
            }
        }

        private void CompleteObjectValue(
            IFieldValueCompletionContext context,
            Action<IFieldValueCompletionContext> nextHandler,
            ObjectType objectType)
        {
            OrderedDictionary objectResult = new OrderedDictionary();
            context.SetResult(objectResult);

            IReadOnlyCollection<FieldSelection> fields = context.ExecutionContext
                .CollectFields(objectType, context.SelectionSet);

            foreach (FieldSelection field in fields)
            {
                context.ExecutionContext.NextBatch.Add(new FieldResolverTask(
                    context.Source.Push(context.Value), objectType, field,
                    context.Path.Append(field.ResponseName), objectResult));
            }
        }

        private ObjectType ResolveObjectType(
            IResolverContext context,
            IType type, object value)
        {
            if (type is ObjectType objectType)
            {
                return objectType;
            }
            else if (type is InterfaceType interfaceType)
            {
                return interfaceType.ResolveType(context, value);
            }
            else if (type is UnionType unionType)
            {
                return unionType.ResolveType(context, value);
            }

            throw new NotSupportedException(
                "The specified type is not supported.");
        }
    }
}
