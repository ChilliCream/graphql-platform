using System;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;

namespace HotChocolate.Client.Core.Builders
{
    internal static class JsonMethods
    {
        public static readonly MethodInfo JTokenAnnotation = GetMethod(typeof(JToken), nameof(JToken.Annotation), new Type[0]);
        public static readonly PropertyInfo JTokenIndexer = typeof(JToken).GetTypeInfo().GetDeclaredProperty("Item");
        public static readonly MethodInfo JTokenSelectToken = GetMethod(typeof(JToken), nameof(JToken.SelectToken), new[] { typeof(string) });
        public static readonly MethodInfo JTokenSelectTokens = GetMethod(typeof(JToken), nameof(JToken.SelectTokens), new[] { typeof(string) });
        public static readonly MethodInfo JTokenToObject = GetMethod(typeof(JToken), nameof(JToken.ToObject), new Type[0]);

        static MethodInfo GetMethod(Type type, string name, params Type[] parameters)
        {
            return type.GetTypeInfo().GetDeclaredMethods(name)
                .First(x => Enumerable.Select(x.GetParameters(), y => y.ParameterType).SequenceEqual(parameters));
        }
    }
}
