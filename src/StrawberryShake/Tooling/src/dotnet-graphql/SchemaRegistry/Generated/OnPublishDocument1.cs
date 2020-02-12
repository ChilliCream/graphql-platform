using System;
using System.Collections;
using System.Collections.Generic;
using StrawberryShake;

namespace StrawberryShake.Tools.SchemaRegistry
{
    [System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
    public partial class OnPublishDocument1
        : IOnPublishDocument
    {
        public OnPublishDocument1(
            global::StrawberryShake.Tools.SchemaRegistry.IPublishDocumentEvent onPublishDocument)
        {
            OnPublishDocument = onPublishDocument;
        }

        public global::StrawberryShake.Tools.SchemaRegistry.IPublishDocumentEvent OnPublishDocument { get; }
    }
}
