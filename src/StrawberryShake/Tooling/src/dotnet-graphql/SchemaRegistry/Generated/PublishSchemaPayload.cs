using System;
using System.Collections;
using System.Collections.Generic;
using StrawberryShake;

namespace StrawberryShake.Tools.SchemaRegistry
{
    [System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
    public class PublishSchemaPayload
        : IPublishSchemaPayload
    {
        public PublishSchemaPayload(
            ISchemaPublishReport report, 
            string? clientMutationId)
        {
            Report = report;
            ClientMutationId = clientMutationId;
        }

        public ISchemaPublishReport Report { get; }

        public string? ClientMutationId { get; }
    }
}
