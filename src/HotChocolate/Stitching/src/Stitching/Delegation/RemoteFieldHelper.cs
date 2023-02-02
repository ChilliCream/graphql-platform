using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Stitching.Delegation;

internal static class RemoteFieldHelper
{
    public static object? RemoteFieldResolver(IPureResolverContext context)
    {
        if (!context.Selection.Field.Directives.ContainsDirective(DirectiveNames.Computed) &&
            context.Parent<object>() is IReadOnlyDictionary<string, object> dict)
        {
            var responseName = context.Selection.SyntaxNode.Alias == null
                ? context.Selection.SyntaxNode.Name.Value
                : context.Selection.SyntaxNode.Alias.Value;

            dict.TryGetValue(responseName, out var obj);
            return DictionaryDeserializer.DeserializeResult(
                context.Selection.Type,
                obj,
                context.Service<InputParser>(),
                context.Path);
        }

        return null;
    }
}

