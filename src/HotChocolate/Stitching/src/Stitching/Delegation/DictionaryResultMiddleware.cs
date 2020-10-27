using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.Resolvers;

namespace HotChocolate.Stitching.Delegation
{
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
                context.Result = s.Data is IDictionary<string, object> d
                    ? d
                    : DictionaryDeserializer.DeserializeResult(context.Field.Type, s.Data);
            }
            else if (context.Result is null &&
                !context.Field.Directives.Contains(DirectiveNames.Computed) &&
                context.Parent<object>() is IDictionary<string, object> dict)
            {
                string responseName = context.FieldSelection.Alias == null
                    ? context.FieldSelection.Name.Value
                    : context.FieldSelection.Alias.Value;

                dict.TryGetValue(responseName, out object? obj);
                context.Result = DictionaryDeserializer.DeserializeResult(context.Field.Type, obj);
            }

            return _next.Invoke(context);
        }
    }
}
