using System;
using System.Globalization;
using System.Threading.Tasks;
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

        public ResolveOperationMiddleware(
            QueryDelegate next,
            Cache<OperationDefinitionNode> queryCache)
        {
            _next = next
                ?? throw new ArgumentNullException(nameof(next));
            _queryCache = queryCache
                ?? new Cache<OperationDefinitionNode>(Defaults.CacheSize);
        }

        public Task InvokeAsync(IQueryContext context)
        {
            string operationName = context.Request.OperationName;
            string cacheKey = CreateKey(operationName, context.QueryKey);

            OperationDefinitionNode node = _queryCache.GetOrCreate(cacheKey,
                () => QueryDocumentHelper.GetOperation(
                    context.Document, operationName));

            ObjectType rootType = ResolveRootType(context, node.Operation);
            object rootValue = ResolveRootValue(context, rootType);
            var disposeRootValue = false;

            if (rootValue == null)
            {
                rootValue = CreateRootValue(context, rootType);
                disposeRootValue = true;
            }

            var variableBuilder = new VariableValueBuilder(
                context.Schema, node);

            context.Operation = new Operation(
                context.Document, node,
                variableBuilder.CreateValues(context.Request.VariableValues),
                rootType, rootValue);

            try
            {
                return _next(context);
            }
            finally
            {
                if (disposeRootValue && rootValue is IDisposable d)
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
