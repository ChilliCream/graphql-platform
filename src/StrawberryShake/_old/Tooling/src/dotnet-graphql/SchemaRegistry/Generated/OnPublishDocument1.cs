using System;
using System.Collections;
using System.Collections.Generic;
using StrawberryShake;

namespace StrawberryShake
{
    [System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
    public partial class OnPublishDocument1
        : IOnPublishDocument
    {
        public OnPublishDocument1(
            global::StrawberryShake.IPublishDocumentEvent onPublishDocument)
        {
            OnPublishDocument = onPublishDocument;
        }

        public global::StrawberryShake.IPublishDocumentEvent OnPublishDocument { get; }
    }
}
