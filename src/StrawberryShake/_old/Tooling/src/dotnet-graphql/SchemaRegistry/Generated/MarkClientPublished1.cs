using System;
using System.Collections;
using System.Collections.Generic;
using StrawberryShake;

namespace StrawberryShake
{
    [System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
    public partial class MarkClientPublished1
        : IMarkClientPublished
    {
        public MarkClientPublished1(
            global::StrawberryShake.IMarkClientPublishedPayload markClientPublished)
        {
            MarkClientPublished = markClientPublished;
        }

        public global::StrawberryShake.IMarkClientPublishedPayload MarkClientPublished { get; }
    }
}
