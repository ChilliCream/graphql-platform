using System;
using HotChocolate.Language;
using HotChocolate.Resolvers;

namespace HotChocolate.Types
{
    public interface IDirective
    {
        string Name { get; }

        DirectiveType Type { get; }

        DirectiveNode Node { get; }

        bool IsMiddleware { get; }

        bool IsResolver { get; }

        T CreateArguments<T>();

        T CreateArgument<T>(string argumentName);

        IDirectiveFieldResolver CreateResolver();

        IDirectiveFieldResolverHandler CreateMiddleware();
    }

    internal sealed class Directive
    {
        public Directive(DirectiveNode directiveNode)
        {

        }

        public Directive(object customDirective)
        {

        }

        public string Name { get; }

        public DirectiveType Type { get; }

        public DirectiveNode Node { get; }

        public bool IsMiddleware { get; }

        public bool IsResolver { get; }

        public T CreateArguments<T>() => throw new NotImplementedException();

        public T CreateArgument<T>(string argumentName) => throw new NotImplementedException();

        public IDirectiveFieldResolver CreateResolver() => throw new NotImplementedException();

        public IDirectiveFieldResolverHandler CreateMiddleware() => throw new NotImplementedException();

        internal void CompleteDirective(DirectiveType directive)
        {

        }

        internal static Directive FromObject()
        {

        }
    }
}
