
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Resolvers;

namespace HotChocolate.Stitching
{
    internal class QueryBroker
    {
        private readonly IStitchingContext _stitchingContext;

        public QueryBroker(IStitchingContext stitchingContext)
        {
            _stitchingContext = stitchingContext
                ?? throw new ArgumentNullException(nameof(stitchingContext));
        }

        public Task<IExecutionResult> RedirectQueryAsync(
            IDirectiveContext directiveContext,
            CancellationToken cancellationToken)
        {
            string schemaName = directiveContext.FieldSelection.GetSchemaName();
            IQueryExecuter queryExecuter =
                _stitchingContext.GetQueryExecuter(schemaName);
            QueryRequest queryRequest = CreateQuery(directiveContext);
            return queryExecuter.ExecuteAsync(queryRequest, cancellationToken);
        }

        private QueryRequest CreateQuery(
            IDirectiveContext directiveContext)
        {
            var rewriter = new ExtractRemoteQueryRewriter();
            var fieldSelection = (FieldNode)rewriter.Rewrite(
                directiveContext.FieldSelection,
                directiveContext.FieldSelection.GetSchemaName());

            var selectionSet = new SelectionSetNode(
                null,
                new List<ISelectionNode> { fieldSelection });

            var operation = new OperationDefinitionNode(
                null,
                null,
                OperationType.Query,
                new List<VariableDefinitionNode>(),
                new List<DirectiveNode>(),
                selectionSet);

            var query = new DocumentNode(
                null,
                new List<IDefinitionNode> { operation });

            var queryText = new StringBuilder();
            using (var stringWriter = new StringWriter(queryText))
            {
                var serializer = new QuerySyntaxSerializer();
                serializer.Visit(query, new DocumentWriter(stringWriter));
            }

            return new QueryRequest(queryText.ToString());
        }
    }
}
