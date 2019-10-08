using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter.Xunit;
using Xunit;
using HotChocolate.Execution;
using HotChocolate.Types;
using HotChocolate.Resolvers;
using FileResource = ChilliCream.Testing.FileResource;
using HotChocolate.AspNetCore.Tests.Utilities;
using System.Threading;

namespace HotChocolate.Stitching
{
    public class DelegateToRemoteSchemaMiddlewareTests
        : StitchingTestBase
    {
        public DelegateToRemoteSchemaMiddlewareTests(
            TestServerFactory testServerFactory)
            : base(testServerFactory)
        {
        }

        [Fact]
        public async Task ExecuteStitchingQueryWithInlineFragment()
        {
            // arrange
            var request = QueryRequestBuilder.New()
                .SetQuery(FileResource.Open(
                    "StitchingQueryWithInlineFragment.graphql"))
                .Create();

            // act
            IExecutionResult result = await ExecuteStitchedQuery(request);

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task ExecuteStitchingQueryWithFragmentDefinition()
        {
            // arrange
            var request = QueryRequestBuilder.New()
                .SetQuery(FileResource.Open(
                    "StitchingQueryWithFragmentDefs.graphql"))
                .Create();

            // act
            IExecutionResult result = await ExecuteStitchedQuery(request);

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task ExecuteStitchingQueryWithVariables()
        {
            // arrange
            var request = QueryRequestBuilder.New()
                .SetQuery(FileResource.Open(
                    "StitchingQueryWithVariables.graphql"))
                .SetVariableValue("customerId", "Q3VzdG9tZXIKZDE=")
                .Create();

            // act
            IExecutionResult result = await ExecuteStitchedQuery(request);

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task ExecuteStitchingQueryWithUnion()
        {
            // arrange
            var request = QueryRequestBuilder.New()
                .SetQuery(FileResource.Open(
                    "StitchingQueryWithUnion.graphql"))
                .Create();

            // act
            IExecutionResult result = await ExecuteStitchedQuery(request);

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task ExecuteStitchingQueryWithArguments()
        {
            // arrange
            var request = QueryRequestBuilder.New()
                .SetQuery(FileResource.Open(
                    "StitchingQueryWithArguments.graphql"))
                .Create();

            // act
            IExecutionResult result = await ExecuteStitchedQuery(request);

            // assert
            result.MatchSnapshot();
        }

        [Fact(Skip = "Not yet supported!")]
        public async Task ExecuteStitchingQueryDeepArrayPath()
        {
            // arrange
            var request = QueryRequestBuilder.New()
                .SetQuery(FileResource.Open(
                    "StitchingQueryDeepArrayPath.graphql"))
                .Create();

            // act
            IExecutionResult result = await ExecuteStitchedQuery(request);

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task ExecuteStitchingQueryDeepObjectPath()
        {
            // arrange
            var request = QueryRequestBuilder.New()
                .SetQuery(FileResource.Open(
                    "StitchingQueryDeepObjectPath.graphql"))
                .Create();

            // act
            IExecutionResult result = await ExecuteStitchedQuery(request);

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task ExecuteStitchingQueryDeepScalarPath()
        {
            // arrange
            var request = QueryRequestBuilder.New()
                .SetQuery(FileResource.Open(
                    "StitchingQueryDeepScalarPath.graphql"))
                .Create();

            // act
            IExecutionResult result = await ExecuteStitchedQuery(request);

            // assert
            result.MatchSnapshot();
        }

        private Task<IExecutionResult> ExecuteStitchedQuery(
            IReadOnlyQueryRequest request)
        {
            // arrange
            IHttpClientFactory clientFactory = CreateRemoteSchemas();

            var serviceCollection = new ServiceCollection();

            serviceCollection.AddSingleton(clientFactory);

            serviceCollection.AddRemoteQueryExecutor(b => b
                .SetSchemaName("contract")
                .SetSchema(FileResource.Open("Contract.graphql"))
                .AddScalarType<DateTimeType>());

            serviceCollection.AddRemoteQueryExecutor(b => b
                .SetSchemaName("customer")
                .SetSchema(FileResource.Open("Customer.graphql")));

            serviceCollection.AddStitchedSchema(
                FileResource.Open("Stitching.graphql"),
                c => c.RegisterType<DateTimeType>());

            IServiceProvider services =
                serviceCollection.BuildServiceProvider();

            request = QueryRequestBuilder.From(request)
                .SetServices(services)
                .Create();

            IQueryExecutor executor = services
                .GetRequiredService<IQueryExecutor>();

            // act
            return executor.ExecuteAsync(request);
        }

        [Fact]
        public async Task ExecuteStitchedQueryWithComputedField()
        {
            // arrange
            IHttpClientFactory clientFactory = CreateRemoteSchemas();

            var serviceCollection = new ServiceCollection();

            serviceCollection.AddSingleton(clientFactory);

            serviceCollection.AddRemoteQueryExecutor(b => b
                .SetSchemaName("contract")
                .SetSchema(FileResource.Open("Contract.graphql"))
                .AddScalarType<DateTimeType>());

            serviceCollection.AddRemoteQueryExecutor(b => b
                .SetSchemaName("customer")
                .SetSchema(FileResource.Open("Customer.graphql")));

            serviceCollection.AddStitchedSchema(
                FileResource.Open("StitchingComputed.graphql"),
                c =>
                {
                    c.Map(new FieldReference("Customer", "foo"),
                        next => context =>
                        {
                            var obj = context
                                .Parent<IReadOnlyDictionary<string, object>>();
                            context.Result = obj["name"] + "_" + obj["id"];
                            return Task.CompletedTask;
                        });
                    c.RegisterType<DateTimeType>();
                });

            IServiceProvider services =
                serviceCollection.BuildServiceProvider();

            var request = QueryRequestBuilder.New()
                .SetQuery(
                    FileResource.Open("StitchingQueryComputedField.graphql"))
                .SetServices(services)
                .Create();

            IQueryExecutor executor = services
                .GetRequiredService<IQueryExecutor>();

            // act
            IExecutionResult result = await executor.ExecuteAsync(request);

            // assert
            Snapshot.Match(result);
        }

        [Fact]
        public async Task DelegateWithIntFieldArgument()
        {
            // arrange
            IHttpClientFactory clientFactory = CreateRemoteSchemas();

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton(clientFactory);
            serviceCollection.AddStitchedSchema(builder =>
                builder.AddSchemaFromHttp("contract")
                    .AddSchemaFromHttp("customer")
                    .AddExtensionsFromString(
                        FileResource.Open("StitchingExtensions.graphql"))
                    .AddSchemaConfiguration(c =>
                        c.RegisterType<PaginationAmountType>()));

            IServiceProvider services =
                serviceCollection.BuildServiceProvider();

            IQueryExecutor executor = services
                .GetRequiredService<IQueryExecutor>();
            IExecutionResult result = null;

            // act
            using (IServiceScope scope = services.CreateScope())
            {
                IReadOnlyQueryRequest request =
                    QueryRequestBuilder.New()
                        .SetQuery(@"
                        {
                            customer(id: ""Q3VzdG9tZXIKZDE="") {
                                int
                            }
                        }")
                        .SetServices(scope.ServiceProvider)
                        .Create();

                result = await executor.ExecuteAsync(request);
            }

            // assert
            Snapshot.Match(result);
        }

        [Fact]
        public async Task DelegateWithGuidFieldArgument()
        {
            // arrange
            IHttpClientFactory clientFactory = CreateRemoteSchemas();

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton(clientFactory);
            serviceCollection.AddStitchedSchema(builder =>
                builder.AddSchemaFromHttp("contract")
                    .AddSchemaFromHttp("customer")
                    .AddExtensionsFromString(
                        FileResource.Open("StitchingExtensions.graphql"))
                    .AddSchemaConfiguration(c =>
                    {
                        c.RegisterExtendedScalarTypes();
                        c.RegisterType<PaginationAmountType>();
                    }));

            IServiceProvider services =
                serviceCollection.BuildServiceProvider();

            IQueryExecutor executor = services
                .GetRequiredService<IQueryExecutor>();
            IExecutionResult result = null;

            // act
            using (IServiceScope scope = services.CreateScope())
            {
                IReadOnlyQueryRequest request =
                    QueryRequestBuilder.New()
                        .SetQuery(@"
                        {
                            customer(id: ""Q3VzdG9tZXIKZDE="") {
                                guid
                            }
                        }")
                        .SetServices(scope.ServiceProvider)
                        .Create();

                result = await executor.ExecuteAsync(request);
            }

            // assert
            Snapshot.Match(result);
        }

        [Fact]
        public async Task ExecuteStitchedQueryBuilder()
        {
            // arrange
            IHttpClientFactory clientFactory = CreateRemoteSchemas();

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton(clientFactory);
            serviceCollection.AddStitchedSchema(builder =>
                builder.AddSchemaFromHttp("contract")
                    .AddSchemaFromHttp("customer")
                    .RenameField("customer",
                        new FieldReference("Customer", "name"), "foo")
                    .AddExtensionsFromString(
                        FileResource.Open("StitchingExtensions.graphql"))
                    .AddSchemaConfiguration(c =>
                        c.RegisterType<PaginationAmountType>()));

            IServiceProvider services =
                serviceCollection.BuildServiceProvider();

            IQueryExecutor executor = services
                .GetRequiredService<IQueryExecutor>();
            IExecutionResult result = null;

            // act
            using (IServiceScope scope = services.CreateScope())
            {
                IReadOnlyQueryRequest request =
                    QueryRequestBuilder.New()
                        .SetQuery(@"
                        {
                            a: customer(id: ""Q3VzdG9tZXIKZDE="") {
                                bar: foo
                                contracts {
                                    id
                                }
                            }

                            b: customer(id: ""Q3VzdG9tZXIKZDE="") {
                                foo
                                contracts {
                                    id
                                }
                            }
                        }")
                        .SetServices(scope.ServiceProvider)
                        .Create();

                result = await executor.ExecuteAsync(request);
            }

            // assert
            Snapshot.Match(result);
        }

        [Fact]
        public async Task ExecuteStitchedQueryBuilderVariableArguments()
        {
            // arrange
            IHttpClientFactory clientFactory = CreateRemoteSchemas();

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton(clientFactory);
            serviceCollection.AddStitchedSchema(builder =>
                builder.AddSchemaFromHttp("contract")
                    .AddSchemaFromHttp("customer")
                    .RenameField("customer",
                        new FieldReference("Customer", "name"), "foo")
                    .AddExtensionsFromString(
                        FileResource.Open("StitchingExtensions.graphql"))
                    .AddSchemaConfiguration(c =>
                        c.RegisterType<PaginationAmountType>()));

            IServiceProvider services =
                serviceCollection.BuildServiceProvider();

            IQueryExecutor executor = services
                .GetRequiredService<IQueryExecutor>();
            IExecutionResult result = null;

            // act
            using (IServiceScope scope = services.CreateScope())
            {
                IReadOnlyQueryRequest request =
                    QueryRequestBuilder.New()
                        .SetQuery(@"
                            query a($id: ID! $bar: String) {
                                contracts(customerId: $id)
                                {
                                    id
                                    customerId
                                    ... foo
                                }
                            }

                            fragment foo on LifeInsuranceContract
                            {
                                foo(bar: $bar)
                            }
                        ")
                        .SetVariableValue("id", "Q3VzdG9tZXIKZDE=")
                        .SetVariableValue("bar", "this variable is passed to remote query!")
                        .SetServices(scope.ServiceProvider)
                        .Create();

                result = await executor.ExecuteAsync(request);
            }

            // assert
            Snapshot.Match(result);
        }

        [Fact]
        public async Task ExecuteStitchedQueryBuilderWithRenamedType()
        {
            // arrange
            IHttpClientFactory clientFactory = CreateRemoteSchemas();

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton(clientFactory);
            serviceCollection.AddStitchedSchema(builder =>
                builder.AddSchemaFromHttp("contract")
                    .AddSchemaFromHttp("customer")
                    .RenameField("customer",
                        new FieldReference("Customer", "name"), "foo")
                    .RenameType("SomeOtherContract", "Other")
                    .RenameType("LifeInsuranceContract", "Life")
                    .AddExtensionsFromString(
                        FileResource.Open("StitchingExtensions.graphql"))
                    .AddSchemaConfiguration(c =>
                        c.RegisterType<PaginationAmountType>()));

            IServiceProvider services =
                serviceCollection.BuildServiceProvider();

            IQueryExecutor executor = services
                .GetRequiredService<IQueryExecutor>();
            IExecutionResult result = null;

            // act
            using (IServiceScope scope = services.CreateScope())
            {
                IReadOnlyQueryRequest request =
                    QueryRequestBuilder.New()
                        .SetQuery(@"
                            query a($id: ID!) {
                                a: customer2(customerId: $id) {
                                    bar: foo
                                    contracts {
                                        id
                                        ... life
                                        ... on Other {
                                            expiryDate
                                        }
                                    }
                                }
                            }

                            fragment life on Life
                            {
                                premium
                            }")
                        .SetVariableValue("id", "Q3VzdG9tZXIKZDE=")
                        .SetServices(scope.ServiceProvider)
                        .Create();

                result = await executor.ExecuteAsync(request);
            }

            // assert
            Snapshot.Match(result);
        }

        [Fact]
        public async Task ExecuteStitchedQueryBuilderWithLocalSchema()
        {
            // arrange
            IHttpClientFactory clientFactory = CreateRemoteSchemas();

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton(clientFactory);
            serviceCollection.AddStitchedSchema(builder =>
                builder.AddSchemaFromHttp("contract")
                    .AddSchemaFromHttp("customer")
                    .AddSchema("hello",
                        Schema.Create(
                            "type Query { hello: String! }",
                            c => c.BindResolver(ctx => "Hello World")
                                .To("Query", "hello")))
                    .RenameField("customer",
                        new FieldReference("Customer", "name"), "foo")
                    .RenameType("SomeOtherContract", "Other")
                    .RenameType("LifeInsuranceContract", "Life")
                    .AddExtensionsFromString(
                        FileResource.Open("StitchingExtensions.graphql"))
                    .AddSchemaConfiguration(c =>
                        c.RegisterType<PaginationAmountType>()));

            IServiceProvider services =
                serviceCollection.BuildServiceProvider();

            IQueryExecutor executor = services
                .GetRequiredService<IQueryExecutor>();
            IExecutionResult result = null;

            // act
            using (IServiceScope scope = services.CreateScope())
            {
                IReadOnlyQueryRequest request =
                    QueryRequestBuilder.New()
                        .SetQuery(@"
                            query a($id: ID!) {
                                a: customer2(customerId: $id) {
                                    bar: foo
                                    contracts {
                                        id
                                        ... life
                                        ... on Other {
                                            expiryDate
                                        }
                                    }
                                }
                                hello
                            }

                            fragment life on Life
                            {
                                premium
                            }

                            ")
                        .SetVariableValue("id", "Q3VzdG9tZXIKZDE=")
                        .SetServices(scope.ServiceProvider)
                        .Create();

                result = await executor.ExecuteAsync(request);
            }

            // assert
            Snapshot.Match(result);
        }

        [Fact]
        public async Task ReplaceField()
        {
            // arrange
            IHttpClientFactory clientFactory = CreateRemoteSchemas();

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton(clientFactory);
            serviceCollection.AddStitchedSchema(builder =>
                builder.AddSchemaFromHttp("contract")
                    .AddSchemaFromHttp("customer")
                    .IgnoreField("customer",
                        new FieldReference("Customer", "name"))
                    .RenameField("customer",
                        new FieldReference("Customer", "street"), "name")
                    .AddSchemaConfiguration(c =>
                        c.RegisterType<PaginationAmountType>()));

            IServiceProvider services =
                serviceCollection.BuildServiceProvider();

            IQueryExecutor executor = services
                .GetRequiredService<IQueryExecutor>();
            IExecutionResult result = null;

            // act
            using (IServiceScope scope = services.CreateScope())
            {
                IReadOnlyQueryRequest request =
                    QueryRequestBuilder.New()
                        .SetQuery(@"
                            query a($id: ID!) {
                                a: customer(id: $id) {
                                    name
                                }
                            }")
                        .SetVariableValue("id", "Q3VzdG9tZXIKZDE=")
                        .SetServices(scope.ServiceProvider)
                        .Create();

                result = await executor.ExecuteAsync(request);
            }

            // assert
            Snapshot.Match(result);
        }

        [Fact]
        public async Task ExtendedScalarAsInAndOutputType()
        {
            // arrange
            IHttpClientFactory clientFactory = CreateRemoteSchemas();

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton(clientFactory);
            serviceCollection.AddStitchedSchema(builder =>
                builder.AddSchemaFromHttp("contract")
                    .AddSchemaFromHttp("customer")
                    .AddSchemaConfiguration(c =>
                    {
                        c.RegisterExtendedScalarTypes();
                    })
                    .AddSchemaConfiguration(c =>
                        c.RegisterType<PaginationAmountType>()));

            IServiceProvider services =
                serviceCollection.BuildServiceProvider();

            IQueryExecutor executor = services
                .GetRequiredService<IQueryExecutor>();
            IExecutionResult result = null;

            // act
            using (IServiceScope scope = services.CreateScope())
            {
                IReadOnlyQueryRequest request =
                    QueryRequestBuilder.New()
                        .SetQuery(@"
                            query a($d: DateTime!) {
                                a: extendedScalar(d: ""2018-01-01T01:00:00.000Z"")
                                b: extendedScalar(d: $d)
                            }")
                        .SetVariableValue("d", "2019-01-01T01:00:00.000Z")
                        .SetServices(scope.ServiceProvider)
                        .Create();

                result = await executor.ExecuteAsync(request);
            }

            // assert
            Snapshot.Match(result);
        }

        [Fact]
        public async Task CustomDirectiveIsPassedOn()
        {
            // arrange
            IHttpClientFactory clientFactory = CreateRemoteSchemas();

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton(clientFactory);
            serviceCollection.AddStitchedSchema(builder =>
                builder.AddSchemaFromHttp("contract")
                    .AddSchemaFromHttp("customer")
                    .AddSchemaConfiguration(c =>
                    {
                        c.RegisterExtendedScalarTypes();
                    })
                    .AddSchemaConfiguration(c =>
                        c.RegisterType<PaginationAmountType>()));

            IServiceProvider services =
                serviceCollection.BuildServiceProvider();

            IQueryExecutor executor = services
                .GetRequiredService<IQueryExecutor>();
            IExecutionResult result = null;

            // act
            using (IServiceScope scope = services.CreateScope())
            {
                IReadOnlyQueryRequest request =
                    QueryRequestBuilder.New()
                        .SetQuery(@"
                            query a($d: DateTime!) {
                                a: extendedScalar(d: ""2018-01-01T01:00:00.000Z"")
                                b: extendedScalar(d: $d)
                                    @custom(d: ""2020-09-01T01:00:00.000Z"")
                            }")
                        .SetVariableValue("d", "2019-01-01T01:00:00.000Z")
                        .SetServices(scope.ServiceProvider)
                        .Create();

                result = await executor.ExecuteAsync(request);
            }

            // assert
            Snapshot.Match(result);
        }

        [Fact]
        public async Task DateTimeIsHandledCorrectly()
        {
            // arrange
            IHttpClientFactory clientFactory = CreateRemoteSchemas();

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton(clientFactory);
            serviceCollection.AddStitchedSchema(builder =>
                builder.AddSchemaFromHttp("contract")
                    .AddSchemaFromHttp("customer")
                    .AddSchemaConfiguration(c =>
                    {
                        c.RegisterExtendedScalarTypes();
                    })
                    .AddSchemaConfiguration(c =>
                        c.RegisterType<PaginationAmountType>()));

            IServiceProvider services =
                serviceCollection.BuildServiceProvider();

            IQueryExecutor executor = services
                .GetRequiredService<IQueryExecutor>();
            IExecutionResult result = null;

            // act
            using (IServiceScope scope = services.CreateScope())
            {
                IReadOnlyQueryRequest request =
                    QueryRequestBuilder.New()
                        .SetQuery(@"
                        query a($d: DateTime!) {
                            a: extendedScalar(d: ""2018-01-01T01:00:00.000Z"")
                            b: extendedScalar(d: $d)
                            c: extendedScalar(d: $d)
                                @custom(d: ""2020-09-01T01:00:00.000Z"")
                        }")
                        .SetVariableValue("d", "2019-01-01T01:00:00.000Z")
                        .SetServices(scope.ServiceProvider)
                        .Create();

                result = await executor.ExecuteAsync(request);
            }

            // assert
            Snapshot.Match(result);
        }

        [Fact]
        public async Task StitchedMutation()
        {
            // arrange
            IHttpClientFactory clientFactory = CreateRemoteSchemas();

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton(clientFactory);
            serviceCollection.AddStitchedSchema(builder =>
                builder.AddSchemaFromHttp("contract")
                    .AddSchemaFromHttp("customer")
                    .AddExtensionsFromString(
                        FileResource.Open("StitchingExtensions.graphql"))
                    .AddSchemaConfiguration(c =>
                        c.RegisterType<PaginationAmountType>()));

            IServiceProvider services =
                serviceCollection.BuildServiceProvider();

            IQueryExecutor executor = services
                .GetRequiredService<IQueryExecutor>();
            IExecutionResult result = null;

            // act
            using (IServiceScope scope = services.CreateScope())
            {
                IReadOnlyQueryRequest request =
                    QueryRequestBuilder.New()
                        .SetQuery(@"
                        mutation {
                            createCustomer(input: { name: ""a"" })
                            {
                                customer {
                                    name
                                    contracts {
                                        id
                                    }
                                }
                            }
                        }")
                        .SetServices(scope.ServiceProvider)
                        .Create();

                result = await executor.ExecuteAsync(request);
            }

            // assert
            Snapshot.Match(result);
        }

        [Fact]
        public async Task StitchedMutationWithRenamedInputType()
        {
            // arrange
            IHttpClientFactory clientFactory = CreateRemoteSchemas();

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton(clientFactory);
            serviceCollection.AddStitchedSchema(builder =>
                builder.AddSchemaFromHttp("contract")
                    .AddSchemaFromHttp("customer")
                    .RenameType("CreateCustomerInput", "CreateCustomerInput2")
                    .AddExtensionsFromString(
                        FileResource.Open("StitchingExtensions.graphql"))
                    .AddSchemaConfiguration(c =>
                        c.RegisterType<PaginationAmountType>()));

            IServiceProvider services =
                serviceCollection.BuildServiceProvider();

            IQueryExecutor executor = services
                .GetRequiredService<IQueryExecutor>();
            IExecutionResult result = null;

            // act
            using (IServiceScope scope = services.CreateScope())
            {
                IReadOnlyQueryRequest request =
                    QueryRequestBuilder.New()
                        .SetQuery(@"
                        mutation {
                            createCustomer(input: { name: ""a"" })
                            {
                                customer {
                                    name
                                    contracts {
                                        id
                                    }
                                }
                            }
                        }")
                        .SetServices(scope.ServiceProvider)
                        .Create();

                result = await executor.ExecuteAsync(request);
            }

            // assert
            Snapshot.Match(result);
        }

        [Fact]
        public async Task StitchedMutationWithRenamedFieldArgument()
        {
            // arrange
            IHttpClientFactory clientFactory = CreateRemoteSchemas();

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton(clientFactory);
            serviceCollection.AddStitchedSchema(builder =>
                builder.AddSchemaFromHttp("contract")
                    .AddSchemaFromHttp("customer")
                    .RenameFieldArgument(
                        "Mutation", "createCustomer", "input", "input2")
                    .AddExtensionsFromString(
                        FileResource.Open("StitchingExtensions.graphql"))
                    .AddSchemaConfiguration(c =>
                        c.RegisterType<PaginationAmountType>()));

            IServiceProvider services =
                serviceCollection.BuildServiceProvider();

            IQueryExecutor executor = services
                .GetRequiredService<IQueryExecutor>();
            IExecutionResult result = null;

            // act
            using (IServiceScope scope = services.CreateScope())
            {
                IReadOnlyQueryRequest request =
                    QueryRequestBuilder.New()
                        .SetQuery(@"
                        mutation {
                            createCustomer(input2: { name: ""a"" })
                            {
                                customer {
                                    name
                                    contracts {
                                        id
                                    }
                                }
                            }
                        }")
                        .SetServices(scope.ServiceProvider)
                        .Create();

                result = await executor.ExecuteAsync(request);
            }

            // assert
            Snapshot.Match(result);
        }

        [Fact]
        public async Task StitchedMutationWithRenamedInputField()
        {
            // arrange
            var requestBuilder = new QueryRequestBuilder();
            requestBuilder.SetQuery(@"
                mutation {
                    createCustomer(input: { foo: ""a"" })
                    {
                        customer {
                            name
                            contracts {
                                id
                            }
                        }
                    }
                }");

            // act
            IExecutionResult result =
                await ExecutedMutationWithRenamedInputField(
                    requestBuilder);

            // assert
            Snapshot.Match(result);
        }

        [Fact]
        public async Task StitchedMutationWithRenamedInputFieldList()
        {
            // arrange
            var requestBuilder = new QueryRequestBuilder();
            requestBuilder.SetQuery(@"
                mutation {
                    createCustomers(inputs: [{ foo: ""a"" } { foo: ""b"" }])
                    {
                        customer {
                            name
                            contracts {
                                id
                            }
                        }
                    }
                }");

            // act
            IExecutionResult result =
                await ExecutedMutationWithRenamedInputField(
                    requestBuilder);

            // assert
            Snapshot.Match(result);
        }

        [Fact]
        public async Task StitchedMutationWithRenamedInputFieldInVariables()
        {
            // arrange
            var requestBuilder = new QueryRequestBuilder();
            requestBuilder.SetQuery(@"
                mutation a($input: CreateCustomerInput) {
                    createCustomer(input: $input)
                    {
                        customer {
                            name
                            contracts {
                                id
                            }
                        }
                    }
                }");
            requestBuilder.AddVariableValue("input",
                new Dictionary<string, object>
                {
                    { "foo", "abc" }
                });

            // act
            IExecutionResult result =
                await ExecutedMutationWithRenamedInputField(
                    requestBuilder);

            // assert
            Snapshot.Match(result);
        }

        [Fact]
        public async Task StitchedMutationWithRenamedInputFieldInVariablesList()
        {
            // arrange
            var requestBuilder = new QueryRequestBuilder();
            requestBuilder.SetQuery(@"
                mutation a($input: [CreateCustomerInput]) {
                    createCustomers(inputs: $input)
                    {
                        customer {
                            name
                            contracts {
                                id
                            }
                        }
                    }
                }");
            requestBuilder.AddVariableValue("input",
                new List<object>
                {
                    new Dictionary<string, object>
                    {
                        { "foo", "abc" }
                    },
                    new Dictionary<string, object>
                    {
                        { "foo", "def" }
                    }
                });

            // act
            IExecutionResult result =
                await ExecutedMutationWithRenamedInputField(
                    requestBuilder);

            // assert
            Snapshot.Match(result);
        }

        [Fact]
        public async Task Query_WithEnumArgument_EnumIsCorrectlyPassed()
        {
            // arrange
            IHttpClientFactory clientFactory = CreateRemoteSchemas();

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton(clientFactory);
            serviceCollection.AddStitchedSchema(builder =>
                builder.AddSchemaFromHttp("contract")
                    .AddSchemaFromHttp("customer")
                    .AddSchemaConfiguration(c =>
                        c.RegisterType<PaginationAmountType>()));

            IServiceProvider services =
                serviceCollection.BuildServiceProvider();

            IQueryExecutor executor = services
                .GetRequiredService<IQueryExecutor>();
            IExecutionResult result = null;

            // act
            using (IServiceScope scope = services.CreateScope())
            {
                IReadOnlyQueryRequest request =
                    QueryRequestBuilder.New()
                        .SetQuery(@"
                        {
                            standard: customerByKind(kind: STANDARD)
                            {
                                id
                                kind
                            }

                            premium: customerByKind(kind: PREMIUM)
                            {
                                id
                                kind
                            }
                        }")
                        .SetServices(scope.ServiceProvider)
                        .Create();

                result = await executor.ExecuteAsync(request);
            }

            // assert
            Snapshot.Match(result);
        }

        [Fact]
        public async Task HttpErrorsHavePathSet()
        {
            // arrange
            var connections = new Dictionary<string, HttpClient>();
            IHttpClientFactory clientFactory = CreateRemoteSchemas(connections);

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton(clientFactory);
            serviceCollection.AddStitchedSchema(builder =>
                builder.AddSchemaFromHttp("contract")
                    .AddSchemaFromHttp("customer")
                    .AddExtensionsFromString(
                        FileResource.Open("StitchingExtensions.graphql"))
                    .AddSchemaConfiguration(c =>
                        c.RegisterType<PaginationAmountType>()));

            IServiceProvider services =
                serviceCollection.BuildServiceProvider();

            ISchema schema = services.GetRequiredService<ISchema>();
            IQueryExecutor executor = services
                .GetRequiredService<IQueryExecutor>();
            IExecutionResult result = null;

            // have to replace the http client after the schema is built
            connections["customer"] = new HttpClient(new ServiceUnavailableDelegatingHandler())
            {
                BaseAddress = connections["customer"].BaseAddress
            };


            // act
            using (IServiceScope scope = services.CreateScope())
            {
                IReadOnlyQueryRequest request =
                    QueryRequestBuilder.New()
                        .SetQuery(@"
                        {
                            customer(id: ""Q3VzdG9tZXIKZDE="") {
                                contracts {
                                    id
                                }
                            }
                        }")
                        .SetServices(scope.ServiceProvider)
                        .Create();

                result = await executor.ExecuteAsync(request);
            }

            // assert
            result.MatchSnapshot(options => options.IgnoreField("Errors[0].Exception.StackTrace"));
        }

        private class ServiceUnavailableDelegatingHandler : DelegatingHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var response = new HttpResponseMessage(System.Net.HttpStatusCode.ServiceUnavailable);
                return Task.FromResult(response);
            }
        }

        [Fact]
        public async Task AddErrorFilter()
        {
            // arrange
            IHttpClientFactory clientFactory = CreateRemoteSchemas();

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton(clientFactory);
            serviceCollection.AddStitchedSchema(builder =>
                builder.AddSchemaFromHttp("contract")
                    .AddSchemaFromHttp("customer")
                    .AddExtensionsFromString(
                        FileResource.Open("StitchingExtensions.graphql"))
                    .AddSchemaConfiguration(c =>
                        c.RegisterType<PaginationAmountType>())
                    .AddExecutionConfiguration(b =>
                    {
                        b.AddErrorFilter(error =>
                            error.AddExtension("STITCH", "SOMETHING"));
                    }));

            IServiceProvider services =
                serviceCollection.BuildServiceProvider();

            IQueryExecutor executor = services
                .GetRequiredService<IQueryExecutor>();
            IExecutionResult result = null;

            // act
            using (IServiceScope scope = services.CreateScope())
            {
                IReadOnlyQueryRequest request =
                    QueryRequestBuilder.New()
                        .SetQuery(@"
                        {
                            customer(id: ""Q3VzdG9tZXIKZDE="") {
                                contracts {
                                    id
                                    ... on LifeInsuranceContract {
                                        error
                                    }
                                }
                            }
                        }")
                        .SetServices(scope.ServiceProvider)
                        .Create();

                result = await executor.ExecuteAsync(request);
            }

            // assert
            Snapshot.Match(result);
        }

        public async Task<IExecutionResult>
            ExecutedMutationWithRenamedInputField(
                IQueryRequestBuilder requestBuilder)
        {
            IHttpClientFactory clientFactory = CreateRemoteSchemas();

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton(clientFactory);
            serviceCollection.AddStitchedSchema(builder =>
                builder.AddSchemaFromHttp("contract")
                    .AddSchemaFromHttp("customer")
                    .RenameField(
                        new FieldReference("CreateCustomerInput", "name"),
                        "foo")
                    .AddExtensionsFromString(
                        FileResource.Open("StitchingExtensions.graphql"))
                    .AddSchemaConfiguration(c =>
                    {
                        c.RegisterType<PaginationAmountType>();
                        c.RegisterExtendedScalarTypes();
                    }));

            IServiceProvider services =
                serviceCollection.BuildServiceProvider();

            IQueryExecutor executor = services
                .GetRequiredService<IQueryExecutor>();

            using (IServiceScope scope = services.CreateScope())
            {

                requestBuilder.SetServices(scope.ServiceProvider);
                return await executor.ExecuteAsync(requestBuilder.Create());
            }
        }
    }
}
