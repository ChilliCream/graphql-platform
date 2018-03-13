using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using GraphQLParser.AST;
using Prometheus.Resolvers;

namespace Prometheus.Execution
{
    internal class ResolveSelectionTask
        : IResolveSelectionTask
    {
        private object _result;
        private readonly Action<object> _addValueToResultMap;

        public ResolveSelectionTask(
            IResolverContext context,
            IOptimizedSelection selection,
            Action<object> addValueToResultMap)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
            Selection = selection ?? throw new ArgumentNullException(nameof(selection));
            _addValueToResultMap = addValueToResultMap ?? throw new ArgumentNullException(nameof(addValueToResultMap));
        }

        public IResolverContext Context { get; }

        public IOptimizedSelection Selection { get; }

        public object Result => _result;

        public async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            _result = await Selection.Resolver(Context, cancellationToken);
        }

        public void IntegrateResult(object finalResult)
        {
            _addValueToResultMap(finalResult);
        }
    }
}