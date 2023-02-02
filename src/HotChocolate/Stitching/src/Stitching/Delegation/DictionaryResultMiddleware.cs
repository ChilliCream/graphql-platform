using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Stitching.Delegation;

public class DictionaryResultMiddleware
{
    private readonly FieldDelegate _next;

    public DictionaryResultMiddleware(FieldDelegate next)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
    }

    public ValueTask InvokeAsync(IMiddlewareContext context)
    {
        if (context.Result is SerializedData s)
        {
            context.Result = s.Data is IReadOnlyDictionary<string, object> d
                ? d
                : DictionaryDeserializer.DeserializeResult(
                    context.Selection.Type,
                    s.Data,
                    context.Service<InputParser>(),
                    context.Path);
        }
        else if (context.Result is null &&
            !context.Selection.Field.Directives.ContainsDirective(DirectiveNames.Computed) &&
            context.Parent<object>() is IReadOnlyDictionary<string, object> dict)
        {
            var responseName = context.Selection.SyntaxNode.Alias == null
                ? context.Selection.SyntaxNode.Name.Value
                : context.Selection.SyntaxNode.Alias.Value;

            dict.TryGetValue(responseName, out var obj);
            context.Result = DictionaryDeserializer.DeserializeResult(
                context.Selection.Type,
                obj,
                context.Service<InputParser>(),
                context.Path);
        }

        return _next.Invoke(context);
    }
}
