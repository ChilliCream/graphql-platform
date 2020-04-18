using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Execution;
using Xunit;

namespace HotChocolate.Types.Filters
{
    public class IntegrationTestbase
    {
        public void Expect<T>(
            string where,
            T[] values,
            string fields,
            params Action<IReadOnlyDictionary<string, object>>[] elementInspectors)
            where T : class
                => Expect(null, where, values, fields, elementInspectors);

        public void Expect<T>(
                string testId,
                string where,
                T[] values,
                string fields,
                params Action<IReadOnlyDictionary<string, object>>[] elementInspectors)
                where T : class
        {
            // arrange  
            // act 
            var suffix = testId != null ? "_" + testId : "";

            ISchema schema = SchemaBuilder.New()
                .AddQueryType(
                    d => d.Field("foos")
                        .Resolver(values)
                        .UseFiltering())
                .Create();

            IQueryExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = executor.Execute(
                "{ foos(where: " + where + ") { " + fields + " } }");

            // assert
            var queryResult = (IReadOnlyQueryResult)result;

            Assert.Equal(0, queryResult.Errors?.Count ?? 0);

            var results = queryResult.Data["foos"] as List<object>;

            Assert.NotNull(results);

            Assert.True(results.All(x => x is IReadOnlyDictionary<string, object>));

            Assert.Collection(
                results.OfType<IReadOnlyDictionary<string, object>>(),
                elementInspectors);
        }

        public T[] Items<T>(params T[] items) => items;
    }
}
