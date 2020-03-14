using System;
using System.Collections;
using System.Collections.Generic;
using StrawberryShake;

namespace StrawberryShake
{
    [System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
    public partial class MarkClientPublishedOperation
        : IOperation<IMarkClientPublished>
    {
        public string Name => "markClientPublished";

        public IDocument Document => Queries.Default;

        public OperationKind Kind => OperationKind.Mutation;

        public Type ResultType => typeof(IMarkClientPublished);

        public Optional<string> ExternalId { get; set; }

        public Optional<string> SchemaName { get; set; }

        public Optional<string> EnvironmentName { get; set; }

        public IReadOnlyList<VariableValue> GetVariableValues()
        {
            var variables = new List<VariableValue>();

            if (ExternalId.HasValue)
            {
                variables.Add(new VariableValue("externalId", "String", ExternalId.Value));
            }

            if (SchemaName.HasValue)
            {
                variables.Add(new VariableValue("schemaName", "String", SchemaName.Value));
            }

            if (EnvironmentName.HasValue)
            {
                variables.Add(new VariableValue("environmentName", "String", EnvironmentName.Value));
            }

            return variables;
        }
    }
}
