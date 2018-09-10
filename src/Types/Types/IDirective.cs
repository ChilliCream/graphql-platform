using System;
using HotChocolate.Language;
using HotChocolate.Resolvers;

namespace HotChocolate.Types
{
    public interface IDirective
    {
        string Name { get; }

        DirectiveType Type { get; }

        Type ClrType { get; }

        DirectiveNode Node { get; }

        bool IsMiddleware { get; }

        bool IsResolver { get; }

        IDirectiveFieldResolver CreateResolver();

        IDirectiveFieldResolverHandler CreateMiddleware();

        T CreateArguments<T>();

        T CreateArgument<T>(string argumentName);
    }
}
