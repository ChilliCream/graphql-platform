using System;
using System.Collections;
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
            if (context.Result is SerializedData s)
            {
                context.Result = s.Data is IDictionary<string, object> d
                    ? d
                    : DeserializeResult(context.Field, s.Data);
            }
            else if (context.Result is null
                && !context.Field.Directives.Contains(DirectiveNames.Computed)
                && context.Parent<object>() is IDictionary<string, object> dict)
            {
                string responseName = context.FieldSelection.Alias == null
                    ? context.FieldSelection.Name.Value
                    : context.FieldSelection.Alias.Value;

                dict.TryGetValue(responseName, out object obj);
                context.Result = DeserializeResult(context.Field, obj);
            }

            return _next.Invoke(context);
        }

        private static object DeserializeResult(
            IOutputField field,
            object obj)
        {
            if (field.Type.IsLeafType()
                && field.Type.NamedType() is ISerializableType t
                && t.TryDeserialize(obj, out object value))
            {
                return value;
            }
            else if (field.Type.IsListType()
                && field.Type.NamedType() is ILeafType leafType
                && obj is IList list)
            {
                Array array = Array.CreateInstance(
                    leafType.ClrType, list.Count);

                for (int i = 0; i < list.Count; i++)
                {
                    array.SetValue(leafType.Deserialize(list[i]), i);
                }

                return array;
            }
            else
            {
                return obj;
            }
        }
    }
}
