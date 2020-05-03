using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Utilities;
using HotChocolate.Execution.Utilities;

namespace HotChocolate.Execution
{
    internal partial class MiddlewareContext : IMiddlewareContext
    {
        private object? _result;

        // todo: we should deprecate this one
        public IImmutableStack<object?> Source => throw new NotImplementedException();

        public Path Path => throw new NotImplementedException();

        public IImmutableDictionary<string, object?> ScopedContextData { get; set; }

        public IImmutableDictionary<string, object?> LocalContextData { get; set; }

        public object? Result
        {
            get => _result;
            set
            {
                _result = value;
                IsResultModified = true;
            }
        }

        public bool IsResultModified { get; private set; }

        public void ModifyScopedContext(ModifyScopedContext modify)
        {
            ScopedContextData = modify(ScopedContextData);
        }

        public T Parent<T>()
        {
            throw new NotImplementedException();
        }
    }
}
