using HotChocolate.Language;
using HotChocolate.Resolvers;

namespace HotChocolate.Types
{
    public interface IDirective
    {
        string Name { get; }

        DirectiveType Type { get; }

        object Source { get; }

        OnBeforeInvokeResolverAsync OnBeforeInvokeResolver { get; }

        OnInvokeResolverAsync OnInvokeResolver { get; }

        OnAfterInvokeResolverAsync OnAfterInvokeResolver { get; }

        bool IsExecutable { get; }

        T ToObject<T>();

        DirectiveNode ToNode();

        T GetArgument<T>(string argumentName);
    }
}
