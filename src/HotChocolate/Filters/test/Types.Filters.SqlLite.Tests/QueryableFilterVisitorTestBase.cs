using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Execution;
using HotChocolate.Resolvers;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types.Filters
{
    public class QueryableFilterVisitorTestBase
    {
        private readonly SqlServerProvider _provider;

        public QueryableFilterVisitorTestBase(SqlServerProvider provider)
        {
            _provider = provider;
        }

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
                => Expect(testId, "sql", where, values, fields, elementInspectors);

        public void Expect<T>(
            string testId,
            string sqlNameExtension,
            string where,
            T[] values,
            string fields,
            params Action<IReadOnlyDictionary<string, object>>[] elementInspectors)
            where T : class
        {
            // arrange  
            // act 
            var suffix = testId != null ? "_" + testId : "";
            IServiceCollection serviceCollection;
            Func<IResolverContext, IEnumerable<T>> resolver;
            (serviceCollection, resolver) = _provider.CreateResolver(values);
            serviceCollection.AddSingleton<MatchSqlHelper>();
            ServiceProvider sp = serviceCollection.BuildServiceProvider();

            ISchema schema = SchemaBuilder.New()
                .AddServices(sp)
                .AddQueryType(
                    d => d.Field("foos")
                        .Resolver(resolver)
                        .MatchSql<T>()
                        .UseFiltering())
                .Create();

            IQueryExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = executor.Execute(
                "{ foos(where: " + where + ") { " + fields + " } }");

            // assert
            var queryResult = (IReadOnlyQueryResult)result;

            Assert.Equal(0, (queryResult.Errors?.Count ?? 0));

            var results = queryResult.Data["foos"] as List<object>;

            Assert.NotNull(results);

            Assert.True(results.All(x => x is IReadOnlyDictionary<string, object>));

            sp.GetService<MatchSqlHelper>().AssertSnapshot(sqlNameExtension);

            Assert.Collection(
                results.OfType<IReadOnlyDictionary<string, object>>(),
                elementInspectors);

            result.MatchSnapshot(new SnapshotNameExtension(suffix));
        }

        public T[] Items<T>(params T[] items) => items;
    }
}
