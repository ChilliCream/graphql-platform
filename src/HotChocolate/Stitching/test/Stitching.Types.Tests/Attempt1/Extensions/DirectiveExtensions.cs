using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Types.Attempt1;

public static class DirectiveExtensions
{
    public static T ModifyDirectives<T>(this T obj, DirectiveNode? add = default,
        DirectiveNode? remove = default)
        where T : IHasDirectives, IHasWithDirectives<T>
    {
        DirectiveNode[]? addArray = add is null
            ? default
            : new[] { add };

        DirectiveNode[]? removeArray = remove is null
            ? default
            : new[] { remove };

        return ModifyDirectives(obj, add: addArray, remove: removeArray);
    }

    public static T ModifyDirectives<T>(this T obj, IEnumerable<DirectiveNode>? add = default, IEnumerable<DirectiveNode>? remove = default)
        where T : IHasDirectives, IHasWithDirectives<T>
    {
        add ??= Enumerable.Empty<DirectiveNode>();
        remove ??= Enumerable.Empty<DirectiveNode>();

        IEnumerable<DirectiveNode> modifiedDirectives = obj.Directives
            .Where(x => !remove.Contains(x))
            .Concat(add)
            .Distinct();

        return obj.WithDirectives(modifiedDirectives.ToList());
    }
}
