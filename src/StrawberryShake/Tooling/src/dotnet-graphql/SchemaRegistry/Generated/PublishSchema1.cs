using System;
using System.Collections;
using System.Collections.Generic;
using StrawberryShake;

namespace StrawberryShake.Tools.SchemaRegistry
{
    [System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
    public class PublishSchema1
        : IPublishSchema
    {
        public PublishSchema1(
            IPublishSchemaPayload publishSchema)
        {
            PublishSchema = publishSchema;
        }

        public IPublishSchemaPayload PublishSchema { get; }
    }
}
