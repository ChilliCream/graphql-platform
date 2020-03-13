using System;
using System.Collections;
using System.Collections.Generic;
using StrawberryShake;

namespace StrawberryShake
{
    [System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
    public partial class PublishClientOperation
        : IOperation<IPublishClient>
    {
        public string Name => "publishClient";

        public IDocument Document => Queries.Default;

        public OperationKind Kind => OperationKind.Mutation;

        public Type ResultType => typeof(IPublishClient);

        public Optional<string> ExternalId { get; set; }

        public Optional<string> SchemaName { get; set; }

        public Optional<string> EnvironmentName { get; set; }

        public Optional<string> ClientName { get; set; }

        public Optional<QueryFileFormat> Format { get; set; }

        public Optional<global::System.Collections.Generic.IReadOnlyList<global::StrawberryShake.QueryFileInput>> Files { get; set; }

        public Optional<global::System.Collections.Generic.IReadOnlyList<global::StrawberryShake.TagInput>?> Tags { get; set; }

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

            if (ClientName.HasValue)
            {
                variables.Add(new VariableValue("clientName", "String", ClientName.Value));
            }

            if (Format.HasValue)
            {
                variables.Add(new VariableValue("format", "QueryFileFormat", Format.Value));
            }

            if (Files.HasValue)
            {
                variables.Add(new VariableValue("files", "QueryFileInput", Files.Value));
            }

            if (Tags.HasValue)
            {
                variables.Add(new VariableValue("tags", "TagInput", Tags.Value));
            }

            return variables;
        }
    }
}
