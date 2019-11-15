using System.Linq;
using System.Reflection;

namespace StrawberryShake.Http.Subscriptions
{
    public static class Subscription
    {
        private static MethodInfo _genericNew = typeof(Subscription)
            .GetMethods(BindingFlags.Static)
            .Single(t => t.IsGenericMethodDefinition);

        public static ISubscription New<T>(
            IOperation operation,
            IResultParser parser)
            where T : class =>
            new Subscription<T>(operation, parser);

        public static ISubscription New(
            IOperation operation,
            IResultParser parser)
        {
            return (ISubscription)_genericNew
                .MakeGenericMethod(new[] { parser.ResultType })
                .Invoke(null, new object[] { operation, parser })!;
        }
    }
}
