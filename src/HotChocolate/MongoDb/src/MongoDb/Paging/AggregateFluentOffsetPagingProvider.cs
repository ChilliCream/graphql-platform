using Cashflow.Cloud.GraphQLServer.MongoDb.Execution;
using HotChocolate.Internal;
using HotChocolate.Types.Pagination;
using MongoDB.Driver;
using System;
using System.Linq;
using System.Reflection;

namespace HotChocolate.MongoDb.Paging
{
    public class AggregateFluentOffsetPagingProvider : OffsetPagingProvider
    {
        private static readonly MethodInfo _createHandler =
            typeof(AggregateFluentOffsetPagingProvider).GetMethod(
                nameof(CreateHandlerInternal),
                BindingFlags.Static | BindingFlags.NonPublic)!;

        public override bool CanHandle(IExtendedType source)
        {
            if (source.Type.IsGenericType && source.Type.GetGenericTypeDefinition() == typeof(IAggregateFluentExecutable<>))
            {
                return true;
            }
            else
            {
                return source.Type.GetInterfaces().Any(x =>
                  x.IsGenericType &&
                  x.GetGenericTypeDefinition() == typeof(IAggregateFluentExecutable<>));
            }
        }

        protected override OffsetPagingHandler CreateHandler(
            IExtendedType source,
            PagingOptions options)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return (OffsetPagingHandler)_createHandler
                .MakeGenericMethod(source.TypeArguments.First().Type)
                .Invoke(null, new object[] { options })!;
        }

        private static AggregateFluentOffsetPagingHandler<TEntity> CreateHandlerInternal<TEntity>(
            PagingOptions options) =>
            new AggregateFluentOffsetPagingHandler<TEntity>(options);
    }
}
