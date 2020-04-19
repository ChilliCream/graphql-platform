using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Resolvers;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types.Filters
{
    public class QueryableFilterTests :
        IClassFixture<SqlServerProvider>
    {
        private static readonly List<FooDateTime> _dataDateTime = new List<FooDateTime>
        {
                new FooDateTime { Foo = new DateTime(2020,01,01, 18, 0, 0, DateTimeKind.Utc) },
                new FooDateTime { Foo = new DateTime(2018,01,01, 18, 0, 0, DateTimeKind.Utc) }
        };

        private static readonly List<Foo> _data = new List<Foo>
        {
                new Foo { Bar = "aa", Baz = 1, Qux = 1 },
                new Foo { Bar = "ba", Baz = 1 },
                new Foo { Bar = "ca", Baz = 2 },
                new Foo { Bar = "ab", Baz = 2 },
                new Foo { Bar = "ac", Baz = 2 },
                new Foo { Bar = "ad", Baz = 2 },
                new Foo { Bar = null, Baz = 0 }
        };

        private readonly SqlServerProvider _provider;

        public QueryableFilterTests(SqlServerProvider provider)
        {
            _provider = provider;
        }

        [Fact]
        public void Create_Schema_With_FilterType_With_Fluent_API()
        {
            // arrange
            IServiceCollection serviceCollection;
            Func<IResolverContext, IEnumerable<Foo>> resolver;
            (serviceCollection, resolver) = _provider.CreateResolverFromList(_data);

            ServiceProvider sp = serviceCollection.BuildServiceProvider();
            // act 
            ISchema schema = SchemaBuilder.New()
                .AddServices(sp)
                .AddQueryType<Query>(d =>
                    d.Field(m => m.Foos)
                        .Resolver(resolver)
                        .UseFiltering<Foo>(f =>
                            f.BindFieldsExplicitly()
                                .Filter(m => m.Bar)
                                .BindFiltersExplicitly()
                                .AllowEquals()))
                .Create();

            // assert 
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Execute_Filter()
        {
            // arrange
            IServiceCollection serviceCollection;
            Func<IResolverContext, IEnumerable<Foo>> resolver;
            (serviceCollection, resolver) = _provider.CreateResolverFromList(_data);
            serviceCollection.AddSingleton<MatchSqlHelper>();
            ServiceProvider sp = serviceCollection.BuildServiceProvider();

            ISchema schema = SchemaBuilder.New()
                .AddServices(sp)
                .AddQueryType<Query>(
                    d => d.Field(t => t.Foos)
                        .Resolver(resolver)
                        .MatchSql<Foo>()
                        .UseFiltering<Foo>())
                .Create();

            IQueryExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = executor.Execute(
                "{ foos(where: { bar_starts_with: \"a\" }) { bar } }");

            // assert
            sp.GetService<MatchSqlHelper>().AssertSnapshot();
            result.MatchSnapshot();
        }

        [Fact]
        public void Execute_Filter_With_Variables()
        {
            // arrange 
            IServiceCollection serviceCollection;
            Func<IResolverContext, IEnumerable<Foo>> resolver;
            (serviceCollection, resolver) = _provider.CreateResolverFromList(_data);
            serviceCollection.AddSingleton<MatchSqlHelper>();
            ServiceProvider sp = serviceCollection.BuildServiceProvider();

            ISchema schema = SchemaBuilder.New()
                .AddServices(sp)
                .AddQueryType<Query>(
                    d => d.Field(t => t.Foos)
                        .Resolver(resolver)
                        .MatchSql<Foo>()
                        .UseFiltering<Foo>())
                .Create();

            IQueryExecutor executor = schema.MakeExecutable();

            IReadOnlyQueryRequest request = QueryRequestBuilder.New()
                .SetQuery(
                    @"query filter($a: String) {
                        foos(where: { bar_starts_with: $a }) {
                            bar
                        }
                    }")
                .SetVariableValue("a", "a")
                .Create();

            // act
            IExecutionResult result = executor.Execute(request);

            // assert
            sp.GetService<MatchSqlHelper>().AssertSnapshot();
            result.MatchSnapshot();
        }

        [Fact]
        public void Execute_Filter_As_Variable()
        {
            // arrange
            IServiceCollection serviceCollection;
            Func<IResolverContext, IEnumerable<Foo>> resolver;
            (serviceCollection, resolver) = _provider.CreateResolverFromList(_data);
            serviceCollection.AddSingleton<MatchSqlHelper>();
            ServiceProvider sp = serviceCollection.BuildServiceProvider();

            ISchema schema = SchemaBuilder.New()
                .AddServices(sp)
                .AddQueryType<Query>(
                    d => d.Field(t => t.Foos)
                        .Resolver(resolver)
                        .UseFiltering<Foo>()
                        .MatchSql<Foo>())
                .Create();

            IQueryExecutor executor = schema.MakeExecutable();

            IReadOnlyQueryRequest request = QueryRequestBuilder.New()
                .SetQuery(
                    @"query filter($a: FooFilter) {
                        foos(where: $a) {
                            bar
                        }
                    }")
                .SetVariableValue("a", new Dictionary<string, object>
                {
                    { "bar_starts_with", "a" }
                })
                .Create();

            // act
            IExecutionResult result = executor.Execute(request);

            // assert
            sp.GetService<MatchSqlHelper>().AssertSnapshot();
            result.MatchSnapshot();
        }

        [Fact]
        public void Execute_Filter_Is_Null()
        {
            // arrange
            IServiceCollection serviceCollection;
            Func<IResolverContext, IEnumerable<Foo>> resolver;
            (serviceCollection, resolver) = _provider.CreateResolverFromList(_data);
            serviceCollection.AddSingleton<MatchSqlHelper>();
            ServiceProvider sp = serviceCollection.BuildServiceProvider();

            ISchema schema = SchemaBuilder.New()
                .AddServices(sp)
                .AddQueryType<Query>(
                    d => d.Field(t => t.Foos)
                        .Resolver(resolver)
                        .UseFiltering<Foo>()
                        .MatchSql<Foo>())
                .Create();

            IQueryExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = executor.Execute(
                "{ foos { bar } }");

            // assert
            sp.GetService<MatchSqlHelper>().AssertSnapshot();
            result.MatchSnapshot();
        }

        [Fact]
        public void Infer_Filter_From_Field()
        {
            // arrange
            IServiceCollection serviceCollection;
            Func<IResolverContext, IEnumerable<Foo>> resolver;
            (serviceCollection, resolver) = _provider.CreateResolverFromList(_data);
            serviceCollection.AddSingleton<MatchSqlHelper>();
            ServiceProvider sp = serviceCollection.BuildServiceProvider();

            ISchema schema = SchemaBuilder.New()
                .AddServices(sp)
                .AddQueryType<Query>(
                    d => d.Field(t => t.Foos)
                        .Resolver(resolver)
                        .MatchSql<Foo>()
                        .UseFiltering())
                .Create();

            IQueryExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = executor.Execute(
                "{ foos(where: { bar_starts_with: \"a\" }) { bar } }");

            // assert
            sp.GetService<MatchSqlHelper>().AssertSnapshot();
            result.MatchSnapshot();
        }

        [Fact]
        public void Execute_Filter_Equals_Null()
        {
            // arrange
            IServiceCollection serviceCollection;
            Func<IResolverContext, IEnumerable<Foo>> resolver;
            (serviceCollection, resolver) = _provider.CreateResolverFromList(_data);
            serviceCollection.AddSingleton<MatchSqlHelper>();
            ServiceProvider sp = serviceCollection.BuildServiceProvider();

            ISchema schema = SchemaBuilder.New()
                .AddServices(sp)
                .AddQueryType<Query>(
                    d => d.Field(t => t.Foos)
                        .Resolver(resolver)
                        .MatchSql<Foo>()
                        .UseFiltering())
                .Create();

            IQueryExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = executor.Execute(
                "{ foos(where: { bar: null }) { bar } }");

            // assert
            sp.GetService<MatchSqlHelper>().AssertSnapshot();
            result.MatchSnapshot();
        }

        [Fact]
        public void Execute_Filter_Not_Equals_Null()
        {
            // arrange
            IServiceCollection serviceCollection;
            Func<IResolverContext, IEnumerable<Foo>> resolver;
            (serviceCollection, resolver) = _provider.CreateResolverFromList(_data);
            serviceCollection.AddSingleton<MatchSqlHelper>();
            ServiceProvider sp = serviceCollection.BuildServiceProvider();

            ISchema schema = SchemaBuilder.New()
                .AddServices(sp)
                .AddQueryType<Query>(
                    d => d.Field(t => t.Foos)
                        .Resolver(resolver)
                        .MatchSql<Foo>()
                        .UseFiltering())
                .Create();

            IQueryExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = executor.Execute(
                "{ foos(where: { bar_not: null }) { bar } }");

            // assert
            sp.GetService<MatchSqlHelper>().AssertSnapshot();
            result.MatchSnapshot();
        }

        [Fact]
        public void Execute_Filter_In()
        {
            // arrange
            IServiceCollection serviceCollection;
            Func<IResolverContext, IEnumerable<Foo>> resolver;
            (serviceCollection, resolver) = _provider.CreateResolverFromList(_data);
            serviceCollection.AddSingleton<MatchSqlHelper>();
            ServiceProvider sp = serviceCollection.BuildServiceProvider();

            ISchema schema = SchemaBuilder.New()
                .AddServices(sp)
                .AddQueryType<Query>(
                    d => d.Field(t => t.Foos)
                        .Resolver(resolver)
                        .MatchSql<Foo>()
                        .UseFiltering())
                .Create();

            IQueryExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = executor.Execute(
                "{ foos(where: { bar_in: [ \"aa\" \"ab\" ] }) { bar } }");

            // assert
            sp.GetService<MatchSqlHelper>().AssertSnapshot();
            result.MatchSnapshot();
        }

        [Fact]
        public void Execute_Filter_Comparable_In()
        {
            // arrange
            IServiceCollection serviceCollection;
            Func<IResolverContext, IEnumerable<Foo>> resolver;
            (serviceCollection, resolver) = _provider.CreateResolverFromList(_data);
            serviceCollection.AddSingleton<MatchSqlHelper>();
            ServiceProvider sp = serviceCollection.BuildServiceProvider();

            ISchema schema = SchemaBuilder.New()
                .AddServices(sp)
                .AddQueryType<Query>(
                    d => d.Field(t => t.Foos)
                        .Resolver(resolver)
                        .MatchSql<Foo>()
                        .UseFiltering())
                .Create();

            IQueryExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = executor.Execute(
                "{ foos(where: { baz_in: [ 1 0 ] }) { bar } }");

            // assert
            sp.GetService<MatchSqlHelper>().AssertSnapshot();
            result.MatchSnapshot();
        }

        [Fact]
        public void Execute_Filter_Nullable_Equals_1()
        {
            // arrange
            IServiceCollection serviceCollection;
            Func<IResolverContext, IEnumerable<Foo>> resolver;
            (serviceCollection, resolver) = _provider.CreateResolverFromList(_data);
            serviceCollection.AddSingleton<MatchSqlHelper>();
            ServiceProvider sp = serviceCollection.BuildServiceProvider();

            ISchema schema = SchemaBuilder.New()
                .AddServices(sp)
                .AddQueryType<Query>(
                    d => d.Field(t => t.Foos)
                        .Resolver(resolver)
                        .MatchSql<Foo>()
                        .UseFiltering())
                .Create();

            IQueryExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = executor.Execute(
                "{ foos(where: { qux: 1 }) { bar qux } }");

            // assert
            sp.GetService<MatchSqlHelper>().AssertSnapshot();
            result.MatchSnapshot();
        }

        [Fact]
        public void Execute_Filter_Nullable_Equals_Null()
        {
            // arrange
            IServiceCollection serviceCollection;
            Func<IResolverContext, IEnumerable<Foo>> resolver;
            (serviceCollection, resolver) = _provider.CreateResolverFromList(_data);
            serviceCollection.AddSingleton<MatchSqlHelper>();
            ServiceProvider sp = serviceCollection.BuildServiceProvider();

            ISchema schema = SchemaBuilder.New()
                .AddServices(sp)
                .AddQueryType<Query>(
                    d => d.Field(t => t.Foos)
                        .Resolver(resolver)
                        .MatchSql<Foo>()
                        .UseFiltering())
                .Create();

            IQueryExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = executor.Execute(
                "{ foos(where: { qux: null }) { bar qux } }");

            // assert
            sp.GetService<MatchSqlHelper>().AssertSnapshot();
            result.MatchSnapshot();
        }

        [Fact]
        public void Execute_Filter_Equals_And()
        {
            // arrange
            IServiceCollection serviceCollection;
            Func<IResolverContext, IEnumerable<Foo>> resolver;
            (serviceCollection, resolver) = _provider.CreateResolverFromList(_data);
            serviceCollection.AddSingleton<MatchSqlHelper>();
            ServiceProvider sp = serviceCollection.BuildServiceProvider();

            ISchema schema = SchemaBuilder.New()
                .AddServices(sp)
                .AddQueryType<Query>(
                    d => d.Field(t => t.Foos)
                        .Resolver(resolver)
                        .MatchSql<Foo>()
                        .UseFiltering())
                .Create();

            IQueryExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = executor.Execute(
                "{ foos(where: { AND: [ { bar: \"aa\" } { bar: \"ba\" } ] })" +
                " { bar } }");

            // assert
            sp.GetService<MatchSqlHelper>().AssertSnapshot();
            result.MatchSnapshot();
        }

        [Fact]
        public void Execute_Filter_Equals_Or()
        {
            // arrange
            IServiceCollection serviceCollection;
            Func<IResolverContext, IEnumerable<Foo>> resolver;
            (serviceCollection, resolver) = _provider.CreateResolverFromList(_data);
            serviceCollection.AddSingleton<MatchSqlHelper>();
            ServiceProvider sp = serviceCollection.BuildServiceProvider();

            ISchema schema = SchemaBuilder.New()
                .AddServices(sp)
                .AddQueryType<Query>(
                    d => d.Field(t => t.Foos)
                        .Resolver(resolver)
                        .MatchSql<Foo>()
                        .UseFiltering())
                .Create();

            IQueryExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = executor.Execute(
                "{ foos(where: { OR: [ { bar: \"aa\" } { bar: \"ba\" } ] })" +
                " { bar } }");

            // assert
            sp.GetService<MatchSqlHelper>().AssertSnapshot();
            result.MatchSnapshot();
        }

        [Fact]
        public async Task Execute_DateTime_Filter()
        {
            // arrange
            IServiceCollection serviceCollection;
            Func<IResolverContext, IEnumerable<FooDateTime>> resolver;
            (serviceCollection, resolver) = _provider.CreateResolverFromList(_dataDateTime);
            serviceCollection.AddSingleton<MatchSqlHelper>();
            ServiceProvider sp = serviceCollection.BuildServiceProvider();

            ISchema schema = SchemaBuilder.New()
                .AddServices(sp)
                .AddQueryType<QueryFooDateTime>(
                    d => d.Field(t => t.Foo)
                        .Resolver(resolver)
                        .MatchSql<FooDateTime>()
                        .UseFiltering())
                .Create();

            IQueryExecutor executor = schema.MakeExecutable();

            IReadOnlyQueryRequest request = QueryRequestBuilder.New()
                .SetQuery("{ foo(where: { foo_gte: \"2019-06-01\"}) { foo } }")
                .Create();

            // act
            IExecutionResult result = await executor.ExecuteAsync(request);

            // assert 
            var queryResult = (IReadOnlyQueryResult)result;

            Assert.Equal(0, (queryResult.Errors?.Count ?? 0));

            var results = queryResult.Data["foo"] as List<object>;

            Assert.NotNull(results);
        }

        [Fact]
        public async Task Execute_DateTime_Filter_With_Variables()
        {
            // arrange
            IServiceCollection serviceCollection;
            Func<IResolverContext, IEnumerable<FooDateTime>> resolver;
            (serviceCollection, resolver) = _provider.CreateResolverFromList(_dataDateTime);
            serviceCollection.AddSingleton<MatchSqlHelper>();
            ServiceProvider sp = serviceCollection.BuildServiceProvider();

            ISchema schema = SchemaBuilder.New()
                .AddServices(sp)
                .AddQueryType<QueryFooDateTime>(
                    d => d.Field(t => t.Foo)
                        .Resolver(resolver)
                        .MatchSql<FooDateTime>()
                        .UseFiltering())
                .Create();

            IQueryExecutor executor = schema.MakeExecutable();

            IReadOnlyQueryRequest request = QueryRequestBuilder.New()
                .SetQuery(
                    "query TestQuery($where: FooDateTimeFilter) {" +
                    "foo(where: $where) { foo } }")
                .SetVariableValue("where", new Dictionary<string, object>
                {
                    { "foo_gte", "2019-06-01" }
                })
                .Create();

            // act
            IExecutionResult result = await executor.ExecuteAsync(request);

            // assert 
            var queryResult = (IReadOnlyQueryResult)result;

            Assert.Equal(0, (queryResult.Errors?.Count ?? 0));

            var results = queryResult.Data["foo"] as List<object>;

            Assert.NotNull(results);
        }

        public class FooDateTime
        {
            [Key]
            public int Id { get; set; }

            public DateTime Foo { get; set; }
        }

        public class QueryFooDateTime
        {
            public IEnumerable<FooDateTime> Foo { get; set; } = new List<FooDateTime>
            {
                new FooDateTime { Foo = new DateTime(2020,01,01, 18, 0, 0, DateTimeKind.Utc) },
                new FooDateTime { Foo = new DateTime(2018,01,01, 18, 0, 0, DateTimeKind.Utc) }
            };
        }

        public class FooObject
        {
            [Key]
            public int Id { get; set; }

            public FooNested FooNested { get; set; }
        }
        public class FooNested
        {
            [Key]
            public int Id { get; set; }

            public string Bar { get; set; }
        }

        public class Query
        {
            public IEnumerable<Foo> Foos { get; } = new[]
            {
                new Foo { Bar = "aa", Baz = 1, Qux = 1 },
                new Foo { Bar = "ba", Baz = 1 },
                new Foo { Bar = "ca", Baz = 2 },
                new Foo { Bar = "ab", Baz = 2 },
                new Foo { Bar = "ac", Baz = 2 },
                new Foo { Bar = "ad", Baz = 2 },
                new Foo { Bar = null, Baz = 0 }
            };
        }

        public class Foo
        {
            [Key]
            public int Id { get; set; }

            public string Bar { get; set; }

            [GraphQLType(typeof(NonNullType<IntType>))]
            public long Baz { get; set; }

            [GraphQLType(typeof(IntType))]
            public int? Qux { get; set; }
        }
    }
}
