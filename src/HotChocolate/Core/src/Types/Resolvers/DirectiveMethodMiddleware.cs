using System.Reflection;

namespace HotChocolate.Resolvers;

internal sealed class DirectiveMethodMiddleware : IDirectiveMiddleware
{
    public DirectiveMethodMiddleware(
        string directiveName,
        Type type,
        MethodInfo method)
    {
        if (string.IsNullOrEmpty(directiveName))
        {
            throw new ArgumentNullException(nameof(directiveName));
        }

        DirectiveName = directiveName;
        Type = type ?? throw new ArgumentNullException(nameof(type));
        Method = method ?? throw new ArgumentNullException(nameof(method));
    }

    public string DirectiveName { get; }
    public Type Type { get; }
    public MethodInfo Method { get; }
}
