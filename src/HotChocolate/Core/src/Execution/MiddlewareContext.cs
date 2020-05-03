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

namespace HotChocolate.Execution
{
    internal partial class MiddlewareContext : IMiddlewareContext
    {
        public ObjectType ObjectType => throw new NotImplementedException();

        public ObjectField Field => throw new NotImplementedException();

        public FieldNode FieldSelection => throw new NotImplementedException();

        public NameString ResponseName => throw new NotImplementedException();

        // todo: we should deprecate this one
        public IImmutableStack<object?> Source => throw new NotImplementedException();

        public Path Path => throw new NotImplementedException();

        public IImmutableDictionary<string, object?> ScopedContextData { get; set; }

        public IImmutableDictionary<string, object?> LocalContextData { get; set; }

        public object? Result { get; set; }

        public bool IsResultModified { get; }

        public T Argument<T>(NameString name)
        {
            throw new NotImplementedException();
        }

        public ValueKind ArgumentKind(NameString name)
        {
            throw new NotImplementedException();
        }


       
        public void ModifyScopedContext(ModifyScopedContext modify)
        {
            ScopedContextData = modify(ScopedContextData);
        }

        public T Parent<T>()
        {
            throw new NotImplementedException();
        }

        public Task<T> ResolveAsync<T>()
        {
            throw new NotImplementedException();
        }

        public T Resolver<T>()
        {
            throw new NotImplementedException();
        }
    }
}
