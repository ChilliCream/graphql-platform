
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Resolvers;

namespace HotChocolate.Types
{
    public interface IDirective
    {
        NameString Name { get; }

        DirectiveType Type { get; }

        object Source { get; }

        IReadOnlyList<DirectiveMiddleware> MiddlewareComponents { get; }

        T ToObject<T>();

        DirectiveNode ToNode();

        DirectiveNode ToNode(bool removeNullArguments);

        T GetArgument<T>(string argumentName);
    }
}
