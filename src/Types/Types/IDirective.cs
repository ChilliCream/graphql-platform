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
}
