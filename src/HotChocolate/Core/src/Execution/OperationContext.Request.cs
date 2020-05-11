using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using GreenDonut;
using HotChocolate.Execution.Utilities;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Utilities;

namespace HotChocolate.Execution
{
    internal sealed partial class OperationContext : IOperationContext
    {
        private IRequestContext _context;

        public ISchema Schema => _context.Schema;

        public IServiceProvider Services => _context.Services;

        public IErrorHandler ErrorHandler => _context.ErrorHandler;

        public IDictionary<string, object?> ContextData => _context.ContextData;

        public CancellationToken RequestAborted => _context.RequestAborted;
    }
}