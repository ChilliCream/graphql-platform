using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using HotChocolate.Execution.Utilities;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Types;
using HotChocolate.Utilities;

namespace HotChocolate.Execution
{
    internal sealed class ResolveOperationMiddleware
    {
        private readonly QueryDelegate _next;
        private readonly Cache<OperationDefinitionNode> _queryCache;

        public ResolveOperationMiddleware(QueryDelegate next)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
        }

        public Task InvokeAsync(IQueryContext context)
        {
            string operationName = context.Request.OperationName;
            string operationId = CreateKey(operationName, context.QueryKey);

            IPreparedOperation operation = context.CachedQuery.GetOrCreate(
                operationId,
                () =>
                {
                    OperationDefinitionNode operationNode =
                        QueryDocumentHelper.GetOperation(context.Document, operationName);

                    ObjectType rootType = ResolveRootType(context, operationNode.Operation);

                    IReadOnlyDictionary<SelectionSetNode, PreparedSelectionSet> selectionSets =
                        FieldCollector.PrepareSelectionSets(
                            context.Schema,
                            new FragmentCollection(context.Schema, context.Document),
                            operationNode);

                    return new PreparedOperation(
                        operationId,
                        context.Document,
                        operationNode,
                        rootType,
                        selectionSets);
                });

            context.Operation = operation;
            context.Variables = new VariableValueBuilder(context.Schema, operation.Definition)
                .CreateValues(context.Request.VariableValues);

            var disposeRootValue = false;
            context.RootValue = ResolveRootValue(context, operation.RootType);

            if (context.RootValue == null)
            {
                context.RootValue = CreateRootValue(context, operation.RootType);
                disposeRootValue = true;
            }
            
            try
            {
                return _next(context);
            }
            finally
            {
                if (disposeRootValue && context.RootValue is IDisposable d)
                {
                    d.Dispose();
                }
            }
        }

        private static string CreateKey(string operationName, string queryText)
        {
            if (string.IsNullOrEmpty(operationName))
            {
                return queryText;
            }
            return $"{operationName}-->{queryText}";
        }

        private static ObjectType ResolveRootType(
            IQueryContext context,
            OperationType operationType)
        {
            ObjectType rootType;

            switch (operationType)
            {
                case OperationType.Query:
                    rootType = context.Schema.QueryType;
                    break;

                case OperationType.Mutation:
                    rootType = context.Schema.MutationType;
                    break;

                case OperationType.Subscription:
                    rootType = context.Schema.SubscriptionType;
                    break;

                default:
                    rootType = null;
                    break;
            }

            if (rootType == null)
            {
                throw new QueryException(string.Format(
                    CultureInfo.CurrentCulture,
                    CoreResources.ResolveRootType_DoesNotExist,
                    operationType));
            }

            return rootType;
        }

        private static object ResolveRootValue(
            IQueryContext context, ObjectType rootType)
        {
            object rootValue = context.Request.InitialValue;
            Type clrType = rootType.ToClrType();

            if (rootValue == null && clrType != typeof(object))
            {
                rootValue = context.Services.GetService(clrType);
            }

            return rootValue;
        }

        private static object CreateRootValue(
            IQueryContext context, ObjectType rootType)
        {
            Type clrType = rootType.ToClrType();

            if (clrType != typeof(object))
            {
                var serviceFactory = new ServiceFactory();
                serviceFactory.Services = context.Services;
                return serviceFactory.CreateInstance(clrType);
            }

            return null;
        }
    }
}
