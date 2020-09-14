﻿using System;
using System.Reflection;
using HotChocolate.Internal;

namespace HotChocolate.Types.Pagination
{
    /// <summary>
    /// Represents the default paging provider for in-memory collections or queryables.
    /// </summary>
    public class QueryableOffsetPagingProvider
        : OffsetPagingProvider
    {
        private static readonly MethodInfo _createHandler =
            typeof(QueryableOffsetPagingProvider).GetMethod(
                nameof(CreateHandlerInternal), 
                BindingFlags.Static | BindingFlags.NonPublic)!;

        public override bool CanHandle(IExtendedType source)
        {
            throw new NotImplementedException();
        }

        protected override OffsetPagingHandler CreateHandler(
            IExtendedType source,
            PagingSettings settings)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return (OffsetPagingHandler)_createHandler
                .MakeGenericMethod(source.ElementType?.Source ?? source.Source)
                .Invoke(null, new object[] { settings })!;
        }

        private static QueryableOffsetPagingHandler<TEntity> CreateHandlerInternal<TEntity>(
            PagingSettings settings) =>
            new QueryableOffsetPagingHandler<TEntity>(settings);
    }
}
