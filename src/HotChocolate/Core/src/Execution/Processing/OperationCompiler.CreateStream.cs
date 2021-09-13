using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Execution.Processing
{
    public sealed partial class OperationCompiler
    {
        private static Func<object, IAsyncEnumerable<object?>> CreateStream(Type type)
        {

        }

        private static async IAsyncEnumerable<object?> CreateStreamFromQueryable(
            IQueryable resolverResult)
        {

        }
    }
}
