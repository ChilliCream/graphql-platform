using System;
using System.Collections.Generic;
using System.Reflection;

namespace HotChocolate.Resolvers.CodeGeneration
{
    internal interface IDirectiveMiddlewareDescriptor
        : IDelegateDescriptor
    {
        /// <summary>
        /// Gets the name of the directive to which this middleware belongs to.
        /// </summary>
        /// <value></value>
        string DirectiveName { get; }

        /// <summary>
        /// Gets the type that holds the middleware method.
        /// </summary>
        Type Type { get; }

        /// <summary>
        /// Gets the middleware method.
        /// </summary>
        /// <value></value>
        MethodInfo Method { get; }

        MiddlewareKind Kind { get; }

        /// <summary>
        /// Gets a collection of argument descriptors
        /// defining the structure of the arguments
        /// that the middleware demands.
        /// </summary>
        IReadOnlyCollection<ArgumentDescriptor> Arguments { get; }

        /// <summary>
        /// Defines if the resolver is an asynchronous resolver.
        /// </summary>
        bool IsAsync { get; }
    }

    internal sealed class DirectiveResolverDescriptor
        : IDirectiveMiddlewareDescriptor
    {
        public string DirectiveName => throw new NotImplementedException();

        public Type Type => throw new NotImplementedException();

        public MethodInfo Method => throw new NotImplementedException();

        public MiddlewareKind Kind => throw new NotImplementedException();

        public IReadOnlyCollection<ArgumentDescriptor> Arguments => throw new NotImplementedException();

        public bool IsAsync => throw new NotImplementedException();
    }

}
