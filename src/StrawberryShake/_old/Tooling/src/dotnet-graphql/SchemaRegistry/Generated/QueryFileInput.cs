using System;
using System.Collections;
using System.Collections.Generic;
using StrawberryShake;

namespace StrawberryShake
{
    [System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
    public partial class QueryFileInput
    {
        public Optional<string?> Hash { get; set; }

        public Optional<string> HashAlgorithm { get; set; }

        public Optional<HashFormat> HashFormat { get; set; }

        public Optional<string> Name { get; set; }

        public Optional<string> SourceText { get; set; }
    }
}
