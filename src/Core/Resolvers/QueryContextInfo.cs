using System;
using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Resolvers
{
    internal class QueryContextInfo
    {
        public QueryContextInfo(
            DocumentNode queryDocument,
            OperationDefinitionNode operationDefinition,
            FieldNode fieldSelection,
            Dictionary<string, object> arguments)
        {
            if (queryDocument == null)
            {
                throw new ArgumentNullException(nameof(queryDocument));
            }

            if (operationDefinition == null)
            {
                throw new ArgumentNullException(nameof(operationDefinition));
            }

            if (fieldSelection == null)
            {
                throw new ArgumentNullException(nameof(fieldSelection));
            }

            if (arguments == null)
            {
                throw new ArgumentNullException(nameof(arguments));
            }

            QueryDocument = queryDocument;
            OperationDefinition = operationDefinition;
            FieldSelection = fieldSelection;
            Arguments = arguments;
        }

        public DocumentNode QueryDocument { get; }

        public OperationDefinitionNode OperationDefinition { get; }

        public FieldNode FieldSelection { get; }

        public Dictionary<string, object> Arguments { get; }
    }

}
