using System;
using System.Threading.Tasks;
using HotChocolate.DataLoader;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using static HotChocolate.Execution.QueryRequestBuilder;

namespace HotChocolate.Stitching.Processing
{
    public class GraphQLFetchCall : IFetchCall
    {
        private readonly BatchDataLoader<IQueryRequest, IQueryResult> _dataLoader;
        private readonly Action<IResolverContext, IQueryRequestBuilder> _setVariables;
        private readonly DocumentNode _query;

        public GraphQLFetchCall(
            BatchDataLoader<IQueryRequest, IQueryResult> dataLoader, 
            Action<IResolverContext, IQueryRequestBuilder> setVariables, 
            DocumentNode query)
        {
            _dataLoader = dataLoader ?? 
                throw new ArgumentNullException(nameof(dataLoader));
            _setVariables = setVariables ?? 
                throw new ArgumentNullException(nameof(setVariables));
            _query = query ?? 
                throw new ArgumentNullException(nameof(query));
        }

        public async ValueTask<IQueryResult> InvokeAsync(IResolverContext context)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            IQueryRequestBuilder builder = New().SetQuery(_query);

            _setVariables(context, builder);

            return await _dataLoader
                .LoadAsync(builder.Create(), context.RequestAborted)
                .ConfigureAwait(false);
        }
    }




}
