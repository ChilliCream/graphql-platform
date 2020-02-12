using System;
using System.Collections;
using System.Collections.Generic;
using StrawberryShake;

namespace StrawberryShake.Tools.SchemaRegistry
{
    [System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
    public partial interface IIssue
    {
        IssueType Type { get; }

        string Code { get; }

        string Message { get; }

        string File { get; }

        global::StrawberryShake.Tools.SchemaRegistry.ILocation Location { get; }

        ResolutionType Resolution { get; }
    }
}
