using System;

namespace HotChocolate.Data.Neo4J.Language.Converters
{
    public class NumberConverter
    {
        private static readonly Type ConvertType = typeof(Convert);

        public string ToQuery(object value)
        {
            return $"{value}";
        }

        public object ToValue(object neoValue, Type target)
        {
            if (neoValue.GetType() == target)
            {
                return neoValue;
            }

            var methodInfo = ConvertType.GetMethod($"To{target.Name}", new[] {neoValue.GetType()});
            return methodInfo?.Invoke(null, new[] {neoValue});
        }
    }
}
