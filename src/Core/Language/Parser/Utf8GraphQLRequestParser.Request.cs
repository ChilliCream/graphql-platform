using System;
using System.Collections.Generic;

namespace HotChocolate.Language
{
    public ref partial struct Utf8GraphQLRequestParser
    {
        private ref struct Request
        {
            public string OperationName { get; set; }

            public string QueryName { get; set; }

            public string QueryHash { get; set; }

            public ReadOnlySpan<byte> Query { get; set; }

            public bool IsQueryNull { get; set; }

            public IReadOnlyDictionary<string, object> Variables { get; set; }

            public IReadOnlyDictionary<string, object> Extensions { get; set; }
        }
    }
}
