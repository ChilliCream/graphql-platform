using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HotChocolate.Utilities
{
    public class CombinedServiceProviderTests
    {
        [Fact]
        public void GetServiceWithoutError()
        {
            /***
            Note
            ==========
            This code is adapted from `HotChocolate.SchemaBuilder.Setup.InitializeInterceptors<T>`,
            which was the next relevant call down "down the stack" in the error traces which
            motivate changes to the subject-under-test (i.e. CombinedServiceProvider).
            ***/
            IServiceProvider stringServices = new DictionaryServiceProvider(
                (typeof(IEnumerable<string>), new List<string>(new[] { "one", "two" }))
            );

            IServiceProvider numberServices = new DictionaryServiceProvider(
                (typeof(IEnumerable<int>), new List<int>(new[] { 1, 2, 3, 4, 5 }))
            );

            IServiceProvider services = new CombinedServiceProvider(stringServices, numberServices);

            switch (services.GetService<IEnumerable<int>>())
            {
                case null:
                    throw new Exception("Could not locate service!");

                case var target:
                    Assert.Equal(15, target.Sum());
                    break;
            }
        }

        [Fact(Skip = "For demonstration purposed only")]
        public void ThrowRefelctionError()
        {
            var actual = AnyFailure(typeof(IEnumerable<int>), Array.Empty<int>());
            Assert.False(actual);
        }

        /// This method simulates a rare -- but possible -- edge case in the
        /// implementation of `CombinedServiceProvider.Any()`. Specifically,
        /// there are occasions wherein the _wrong_ overload of `Enumerable.Any()`
        /// is selected, which results in run-time exception. This method will
        /// fail when run. It is intended for instructional purposes only.
        private bool AnyFailure(Type enumerableType, object enumerable)
        {
            Type genericArgumentType = enumerableType
                .GetTypeInfo()
                .GenericTypeArguments[0];

            MethodInfo info = typeof(Enumerable)
                .GetTypeInfo()
                .DeclaredMethods
                .First(m =>
                    m.IsStatic &&
                    m.Name == nameof(Enumerable.Any) &&
                    m.GetParameters().Length > 1)
                    /***
                     Note
                     ==========
                     There are two overloads of `Any` (arity 1 and arity 2).
                     We only want the one which takes a single argument (arity 1).
                     Explicitly selecting an overload with different arity will
                     cause a TargetParameterCountException error when invoked (below).
                     ***/
                .MakeGenericMethod(genericArgumentType);

            return (bool)info.Invoke(null, new[] { enumerable });
        }
    }
}
