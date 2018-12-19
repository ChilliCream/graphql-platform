using System;
using System.Collections.Generic;
using HotChocolate.Utilities;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Execution
{
    internal class ObjectFieldValueHandler
        : IFieldValueHandler
    {
        public void CompleteValue(
            IFieldValueCompletionContext completionContext,
            Action<IFieldValueCompletionContext> nextHandler)
        {
            if (completionContext.Type.IsObjectType()
                || completionContext.Type.IsInterfaceType()
                || completionContext.Type.IsUnionType())
            {
                ObjectType objectType = ResolveObjectType(
                    completionContext.ResolverContext, completionContext.Type, completionContext.Value);
                if (objectType == null)
                {
                    completionContext.ReportError(QueryError.CreateFieldError(
                        "Could not resolve the schema type from " +
                        $"`{completionContext.Value.GetType().GetTypeName()}`.",
                        completionContext.Path,
                        completionContext.Selection.Selection));
                    return;
                }
                CompleteObjectValue(completionContext, objectType);
            }
            else
            {
                nextHandler?.Invoke(completionContext);
            }
        }

        private void CompleteObjectValue(
            IFieldValueCompletionContext context,
            ObjectType objectType)
        {
            var objectResult = new OrderedDictionary();
            context.IntegrateResult(objectResult);
            context.EnqueueForProcessing(objectType, objectResult);
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
