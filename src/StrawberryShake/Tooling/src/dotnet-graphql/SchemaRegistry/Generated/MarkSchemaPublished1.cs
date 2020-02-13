using System;
using System.Collections;
using System.Collections.Generic;
using StrawberryShake;

namespace StrawberryShake.Tools.SchemaRegistry
{
    [System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
    public partial class MarkSchemaPublished1
        : IMarkSchemaPublished
    {
        public MarkSchemaPublished1(
            global::StrawberryShake.Tools.SchemaRegistry.IMarkSchemaPublishedPayload markSchemaPublished)
        {
            MarkSchemaPublished = markSchemaPublished;
        }

        public global::StrawberryShake.Tools.SchemaRegistry.IMarkSchemaPublishedPayload MarkSchemaPublished { get; }
    }
}
