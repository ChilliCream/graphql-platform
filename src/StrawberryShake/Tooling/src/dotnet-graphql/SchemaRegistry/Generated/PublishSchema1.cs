using System;
using System.Collections;
using System.Collections.Generic;
using StrawberryShake;

namespace StrawberryShake
{
    [System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
    public partial class PublishSchema1
        : IPublishSchema
    {
        public PublishSchema1(
            global::StrawberryShake.IPublishSchemaPayload publishSchema)
        {
            PublishSchema = publishSchema;
        }

        public global::StrawberryShake.IPublishSchemaPayload PublishSchema { get; }
    }
}
