using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Stitching.Properties;
using HotChocolate.Types;
using HotChocolate.Utilities;

namespace HotChocolate.Stitching.Client
{
    internal class BufferedRequest
    {
        private BufferedRequest(
            IQueryRequest request,
            DocumentNode document,
            OperationDefinitionNode operation)
        {
            Request = request;
            Document = document;
            Operation = operation;
            Promise = new TaskCompletionSource<IExecutionResult>(
                TaskCreationOptions.RunContinuationsAsynchronously);
        }

        public IQueryRequest Request { get; }

        public DocumentNode Document { get; }

        public OperationDefinitionNode Operation { get; }

        public TaskCompletionSource<IExecutionResult> Promise { get; }

        public IDictionary<string, string>? Aliases { get; set; }

        public static BufferedRequest Create(IQueryRequest request, ISchema schema)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (request.Query is null)
            {
                throw new ArgumentException(
                    StitchingResources.BufferedRequest_Create_QueryCannotBeNull,
                    nameof(request));
            }

            DocumentNode document =
                request.Query is QueryDocument doc
                    ? doc.Document
                    : Utf8GraphQLParser.Parse(request.Query.AsSpan());

            OperationDefinitionNode operation =
                ResolveOperation(document, request.OperationName);

            request = NormalizeRequest(request, operation, schema);

            return new BufferedRequest(request, document, operation);
        }

        internal static OperationDefinitionNode ResolveOperation(
            DocumentNode document,
            string? operationName)
        {
            OperationDefinitionNode? operation = operationName is null
                ? document.Definitions.OfType<OperationDefinitionNode>().SingleOrDefault()
                : document.Definitions.OfType<OperationDefinitionNode>().SingleOrDefault(
                    t => operationName.EqualsOrdinal(t.Name?.Value));

            if (operation is null)
            {
                // TODO : throw helper
                throw new InvalidOperationException(
                    "The provided remote query does not contain the specified operation." +
                    Environment.NewLine +
                    Environment.NewLine +
                    $"`{document}`");
            }

            return operation;
        }

        private static IQueryRequest NormalizeRequest(
            IQueryRequest request,
            OperationDefinitionNode operation,
            ISchema schema)
        {
            if (request.VariableValues is { Count: > 0 })
            {
                var builder = QueryRequestBuilder.From(request);

                foreach (KeyValuePair<string, object?> variable in request.VariableValues)
                {
                    builder.SetVariableValue(
                        variable.Key,
                        RewriteVariable(operation, variable.Key, variable.Value, schema));
                }
            }

            return request;
        }

        private static IValueNode RewriteVariable(
            OperationDefinitionNode operation,
            string name,
            object value,
            ISchema schema)
        {
            VariableDefinitionNode? variableDefinition =
                operation.VariableDefinitions.FirstOrDefault(t =>
                    string.Equals(t.Variable.Name.Value, name, StringComparison.Ordinal));

            if (variableDefinition is not null &&
                schema.TryGetType(
                    variableDefinition.Type.NamedType().Name.Value,
                    out INamedInputType namedType))
            {
                var variableType = (IInputType)variableDefinition.Type.ToType(namedType);
                return variableType.ParseValue(value);
            }

            // TODO : throw helper
            throw new InvalidOperationException(
                $"The specified variable `{name}` does not exist or is of an " +
                "invalid type.");
        }
    }
}
