using System;
using System.Collections;
using System.Collections.Generic;
using StrawberryShake;

namespace StrawberryShake
{
    [System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
    public partial class MarkClientPublishedPayload
        : IMarkClientPublishedPayload
    {
        public MarkClientPublishedPayload(
            global::StrawberryShake.IEnvironmentName environment)
        {
            Environment = environment;
        }

        public global::StrawberryShake.IEnvironmentName Environment { get; }
    }
}
