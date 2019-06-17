using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;

namespace HotChocolate.Client.Core.Builders
{
    public static class Rewritten
    {
        public static class Value
        {
            public static readonly MethodInfo OfTypeMethod = typeof(Value).GetTypeInfo().GetDeclaredMethod(nameof(OfType));
            public static readonly MethodInfo SelectMethod = typeof(Value).GetTypeInfo().GetDeclaredMethod(nameof(Select));
            public static readonly MethodInfo SelectJTokenMethod = typeof(Value).GetTypeInfo().GetDeclaredMethod(nameof(SelectJToken));
            public static readonly MethodInfo SelectFragmentMethod = typeof(Value).GetTypeInfo().GetDeclaredMethod(nameof(SelectFragment));
            public static readonly MethodInfo SelectListMethod = typeof(Value).GetTypeInfo().GetDeclaredMethod(nameof(SelectList));
            public static readonly MethodInfo SingleMethod = typeof(Value).GetTypeInfo().GetDeclaredMethod(nameof(Single));
            public static readonly MethodInfo SingleOrDefaultMethod = typeof(Value).GetTypeInfo().GetDeclaredMethod(nameof(SingleOrDefault));
            public static readonly MethodInfo SwitchMethod = typeof(Value).GetTypeInfo().GetDeclaredMethod(nameof(Switch));

            public static JToken OfType(JToken source, string typeName)
            {
                return (string)source["__typename"] == typeName ? source : null;
            }

            public static TResult Select<TResult>(JToken source, Func<JToken, TResult> selector)
            {
                return source.Type != JTokenType.Null ? selector(source) : default(TResult);
            }

            public static JToken SelectJToken(JToken source, Func<JToken, JToken> selector)
            {
                return source.Type != JTokenType.Null ? selector(source) : JValue.CreateNull();
            }

            public static TResult SelectFragment<TResult>(JToken source, Func<JToken, TResult> selector)
            {
                return source.Type != JTokenType.Null ? selector(source) : default(TResult);
            }

            public static IEnumerable<TResult> SelectList<TResult>(JToken source, Func<JToken, IEnumerable<TResult>> selector)
            {
                return selector(source);
            }

            public static TResult Single<TResult>(TResult value)
            {
                if (value == null)
                {
                    throw new InvalidOperationException("The value passed to Single was null.");
                }

                return value;
            }

            public static TResult SingleOrDefault<TResult>(TResult value) => value;

            public static TResult Switch<TResult>(JToken source, IDictionary<string, Func<JToken, TResult>> selectors)
            {
                var typename = (string)source["__typename"];

                if (selectors.TryGetValue(typename, out var selector))
                {
                    return selector(source);
                }

                return default;
            }
        }

        public static class List
        {
            public static readonly MethodInfo OfTypeMethod = typeof(List).GetTypeInfo().GetDeclaredMethod(nameof(OfType));
            public static readonly MethodInfo SelectMethod = typeof(List).GetTypeInfo().GetDeclaredMethod(nameof(Select));
            public static readonly MethodInfo ToDictionaryMethod = typeof(List).GetTypeInfo().GetDeclaredMethod(nameof(ToDictionary));
            public static readonly MethodInfo ToListMethod = typeof(List).GetTypeInfo().GetDeclaredMethod(nameof(ToList));
            public static readonly MethodInfo ToSubqueryListMethod = typeof(List).GetTypeInfo().GetDeclaredMethod(nameof(ToSubqueryList));
            public static readonly MethodInfo ToSubqueryDictionaryMethod = typeof(List).GetTypeInfo().GetDeclaredMethod(nameof(ToSubqueryDictionary));

            public static IEnumerable<JToken> OfType(IEnumerable<JToken> source, string typeName)
            {
                return source.Where(x => (string)x["__typename"] == typeName);
            }

            public static IEnumerable<TResult> Select<TResult>(
                IEnumerable<JToken> source,
                Func<JToken, TResult> selector)
            {
                return source.Select(selector);
            }

            public static Dictionary<TKey, TElement> ToDictionary<TKey, TElement>(
                IEnumerable<JToken> source,
                Func<JToken, TKey> keySelector,
                Func<JToken, TElement> elementSelector)
            {
                return source.ToDictionary(keySelector, elementSelector);
            }

            public static List<TResult> ToList<TResult>(IEnumerable<JToken> source)
            {
                return source.Select(x => x.ToObject<TResult>()).ToList();
            }

            public static List<T> ToSubqueryList<T>(
                IEnumerable<T> source,
                ISubqueryRunner context,
                ISubquery subquery)
            {
                var result = source.ToList();
                context.SetQueryResultSink(subquery, x => result.Add((T)x));
                return result;
            }

            public static Dictionary<TKey, TElement> ToSubqueryDictionary<TSource, TKey, TElement>(
                IEnumerable<TSource> source,
                ISubqueryRunner context,
                ISubquery subquery,
                Func<TSource, TKey> keySelector,
                Func<TSource, TElement> elementSelector)
            {
                var result = source.ToDictionary(keySelector, elementSelector);
                context.SetQueryResultSink(
                    subquery,
                    x => result.Add(keySelector((TSource)x), elementSelector((TSource)x)));
                return result;
            }
        }

        public static class Interface
        {
            public static readonly MethodInfo CastMethod = typeof(Interface).GetTypeInfo().GetDeclaredMethod(nameof(Cast));

            public static JToken Cast(JToken source, string typeName)
            {
                var received = (string)source["__typename"];

                if (received == typeName)
                {
                    return source;
                }

                throw new InvalidOperationException($"Cast failed: expected '{typeName}', received '{received}'.");
            }
        }
    }
}
