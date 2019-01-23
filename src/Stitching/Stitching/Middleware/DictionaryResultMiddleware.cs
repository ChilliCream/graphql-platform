using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Stitching
{
    public class DictionaryResultMiddleware
    {
        private readonly FieldDelegate _next;

        public DictionaryResultMiddleware(FieldDelegate next)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
        }

        public Task InvokeAsync(IMiddlewareContext context)
        {
            if (context.Result is null
                && context.Parent<object>() is IDictionary<string, object> dict)
            {
                string responseName = context.FieldSelection.Alias == null
                    ? context.FieldSelection.Name.Value
                    : context.FieldSelection.Alias.Value;

                if (dict.TryGetValue(responseName, out object obj)
                    && context.Field.Type.IsLeafType()
                    && context.Field.Type.NamedType() is ISerializableType t
                    && t.TryDeserialize(obj, out object value))
                {
                    context.Result = value;
                }
                else
                {
                    context.Result = obj;
                }
            }

            return _next.Invoke(context);
        }
    }
}
