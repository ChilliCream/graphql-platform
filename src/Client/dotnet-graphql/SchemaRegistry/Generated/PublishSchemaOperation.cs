﻿using System;
using System.Collections;
using System.Collections.Generic;
using StrawberryShake;

namespace StrawberryShake.Tools.SchemaRegistry
{
    [System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
    public class PublishSchemaOperation
        : IOperation<IPublishSchema>
    {
        public string Name => "publishSchema";

        public IDocument Document => Queries.Default;

        public OperationKind Kind => OperationKind.Mutation;

        public Type ResultType => typeof(IPublishSchema);

        public Optional<string> SchemaName { get; set; }

        public Optional<string> EnvironmentName { get; set; }

        public Optional<string> SourceText { get; set; }

        public Optional<IReadOnlyList<TagInput>?> Tags { get; set; }

        public IReadOnlyList<VariableValue> GetVariableValues()
        {
            var variables = new List<VariableValue>();

            if (SchemaName.HasValue)
            {
                variables.Add(new VariableValue("schemaName", "String", SchemaName.Value));
            }

            if (EnvironmentName.HasValue)
            {
                variables.Add(new VariableValue("environmentName", "String", EnvironmentName.Value));
            }

            if (SourceText.HasValue)
            {
                variables.Add(new VariableValue("sourceText", "String", SourceText.Value));
            }

            if (Tags.HasValue)
            {
                variables.Add(new VariableValue("tags", "TagInput", Tags.Value));
            }

            return variables;
        }
    }
}
