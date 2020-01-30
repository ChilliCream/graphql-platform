using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate
{
    public partial class SchemaErrorBuilder
    {
        private class Error
            : ISchemaError
        {
            public string Message { get; set; }

            public string Code { get; set; }

            public ITypeSystemObject TypeSystemObject { get; set; }

            public IReadOnlyCollection<object> Path { get; set; }

            public ImmutableList<ISyntaxNode> SyntaxNodes { get; set; } =
                ImmutableList<ISyntaxNode>.Empty;

            IReadOnlyCollection<ISyntaxNode> ISchemaError.SyntaxNodes =>
                SyntaxNodes;

            public ImmutableDictionary<string, object> Extensions { get; set; }
                = ImmutableDictionary<string, object>.Empty;

            IReadOnlyDictionary<string, object> ISchemaError.Extensions =>
                Extensions;

            public Exception Exception { get; set; }

            public Error Clone()
            {
                return (Error)MemberwiseClone();
            }
        }
    }
}
