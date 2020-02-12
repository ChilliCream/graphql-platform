using System;
using System.Collections;
using System.Collections.Generic;
using StrawberryShake;

namespace StrawberryShake.Tools.SchemaRegistry
{
    [System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
    public partial class MarkClientPublished
        : IMarkClientPublished
    {
        public MarkClientPublished(
            global::StrawberryShake.Tools.SchemaRegistry.IMarkSchemaPublishedPayload1 markSchemaPublished)
        {
            MarkSchemaPublished = markSchemaPublished;
        }

        public global::StrawberryShake.Tools.SchemaRegistry.IMarkSchemaPublishedPayload1 MarkSchemaPublished { get; }
    }
}
