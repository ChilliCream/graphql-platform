using System;
using System.Collections;
using System.Collections.Generic;
using StrawberryShake;

namespace StrawberryShake
{
    [System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
    public partial class Issue
        : IIssue
    {
        public Issue(
            IssueType type, 
            string code, 
            string message, 
            string file, 
            global::StrawberryShake.ILocation location, 
            ResolutionType resolution)
        {
            Type = type;
            Code = code;
            Message = message;
            File = file;
            Location = location;
            Resolution = resolution;
        }

        public IssueType Type { get; }

        public string Code { get; }

        public string Message { get; }

        public string File { get; }

        public global::StrawberryShake.ILocation Location { get; }

        public ResolutionType Resolution { get; }
    }
}
