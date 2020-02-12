using System;
using System.Collections;
using System.Collections.Generic;
using StrawberryShake;

namespace StrawberryShake.Tools.SchemaRegistry
{
    [System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
    public partial class PublishDocumentEvent
        : IPublishDocumentEvent
    {
        public PublishDocumentEvent(
            bool isCompleted, 
            global::StrawberryShake.Tools.SchemaRegistry.IIssue? issue)
        {
            IsCompleted = isCompleted;
            Issue = issue;
        }

        public bool IsCompleted { get; }

        public global::StrawberryShake.Tools.SchemaRegistry.IIssue? Issue { get; }
    }
}
