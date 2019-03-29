using System.Text;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Net.Cache;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Snapshooter.Xunit;
using Xunit;
using HotChocolate.AspNetCore;
using HotChocolate.Execution;
using HotChocolate.Stitching.Schemas.Contracts;
using HotChocolate.Stitching.Schemas.Customers;
using HotChocolate.Types;
using HotChocolate.Resolvers;
using HotChocolate.Stitching.Delegation;
using FileResource = ChilliCream.Testing.FileResource;
using HotChocolate.Language;
using HotChocolate.Stitching.Utilities;
using HotChocolate.Types.Relay;

namespace HotChocolate.Stitching
{
    public class DelegateToRemoteSchemaMiddlewareTests
        : IClassFixture<TestServerFactory>
    {
        public DelegateToRemoteSchemaMiddlewareTests(
            TestServerFactory testServerFactory)
        {
            TestServerFactory = testServerFactory;
        }

        private TestServerFactory TestServerFactory { get; set; }

        [Fact]
        public async Task ExecuteStitchingQueryWithInlineFragment()
        {
            // arrange
            var request = new QueryRequest(FileResource.Open(
                "StitchingQueryWithInlineFragment.graphql"));

            // act
            IExecutionResult result = await ExecuteStitchedQuery(request);

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task ExecuteStitchingQueryWithFragmentDefinition()
        {
            // arrange
            var request = new QueryRequest(FileResource.Open(
                "StitchingQueryWithFragmentDefs.graphql"));

            // act
            IExecutionResult result = await ExecuteStitchedQuery(request);

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task ExecuteStitchingQueryWithVariables()
        {
            // arrange
            var request = new QueryRequest(FileResource.Open(
                "StitchingQueryWithVariables.graphql"))
            {
                VariableValues = new Dictionary<string, object>
                {
                    {"customerId", "Q3VzdG9tZXIteDE="}
                }
            };

            // act
            IExecutionResult result = await ExecuteStitchedQuery(request);

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task ExecuteStitchingQueryWithUnion()
        {
            // arrange
            var request = new QueryRequest(FileResource.Open(
                "StitchingQueryWithUnion.graphql"));

            // act
            IExecutionResult result = await ExecuteStitchedQuery(request);

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task ExecuteStitchingQueryWithArguments()
        {
            // arrange
            var request = new QueryRequest(FileResource.Open(
                "StitchingQueryWithArguments.graphql"));

            // act
            IExecutionResult result = await ExecuteStitchedQuery(request);

            // assert
            result.MatchSnapshot();
        }

        [Fact(Skip = "Not yet supported!")]
        public async Task ExecuteStitchingQueryDeepArrayPath()
        {
            // arrange
            var request = new QueryRequest(FileResource.Open(
                "StitchingQueryDeepArrayPath.graphql"));

            // act
            IExecutionResult result = await ExecuteStitchedQuery(request);

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task ExecuteStitchingQueryDeepObjectPath()
        {
            // arrange
            var request = new QueryRequest(FileResource.Open(
                "StitchingQueryDeepObjectPath.graphql"));

            // act
            IExecutionResult result = await ExecuteStitchedQuery(request);

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task ExecuteStitchingQueryDeepScalarPath()
        {
            // arrange
            var request = new QueryRequest(FileResource.Open(
                "StitchingQueryDeepScalarPath.graphql"));

            // act
            IExecutionResult result = await ExecuteStitchedQuery(request);

            // assert
            result.MatchSnapshot();
        }

        private Task<IExecutionResult> ExecuteStitchedQuery(
            QueryRequest request)
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
                request.Services =
                serviceCollection.BuildServiceProvider();

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
                            OrderedDictionary obj =
                                context.Parent<OrderedDictionary>();
                            context.Result = obj["name"] + "_" + obj["id"];
                            return Task.CompletedTask;
                        });
                    c.RegisterType<DateTimeType>();
                });

            var request = new QueryRequest(
                FileResource.Open("StitchingQueryComputedField.graphql"));

            IServiceProvider services =
                request.Services =
                serviceCollection.BuildServiceProvider();

            IQueryExecutor executor = services
                .GetRequiredService<IQueryExecutor>();

            // act
            IExecutionResult result = await executor.ExecuteAsync(request);

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
                var request = new QueryRequest(@"
                {
                    a: customer(id: ""Q3VzdG9tZXIteDE="") {
                        bar: foo
                        contracts {
                            id
                        }
                    }

                    b: customer(id: ""Q3VzdG9tZXIteDE="") {
                        foo
                        contracts {
                            id
                        }
                    }
                }");
                request.Services = scope.ServiceProvider;

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
                var request = new QueryRequest(@"
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
                ");
                request.VariableValues = new Dictionary<string, object>
                {
                    {"id", "Q3VzdG9tZXIteDE="},
                    {"bar", "this variable is passed to remote query!"}
                };
                request.Services = scope.ServiceProvider;

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
                var request = new QueryRequest(@"
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
                }");

                request.VariableValues = new Dictionary<string, object>
                {
                    {"id", "Q3VzdG9tZXIteDE="}
                };
                request.Services = scope.ServiceProvider;

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
                var request = new QueryRequest(@"
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

                ");
                request.VariableValues = new Dictionary<string, object>
                {
                    {"id", "Q3VzdG9tZXIteDE="}
                };
                request.Services = scope.ServiceProvider;

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
                var request = new QueryRequest(@"
                query a($id: ID!) {
                    a: customer(id: $id) {
                        name
                    }
                }");
                request.VariableValues = new Dictionary<string, object>
                {
                    {"id", "Q3VzdG9tZXIteDE="}
                };
                request.Services = scope.ServiceProvider;

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
                var request = new QueryRequest(@"
                query a($d: DateTime!) {
                    a: extendedScalar(d: ""2018-01-01T01:00:00.000Z"")
                    b: extendedScalar(d: $d)
                }");
                request.VariableValues = new Dictionary<string, object>
                {
                    {"d", "2019-01-01T01:00:00.000Z"}
                };
                request.Services = scope.ServiceProvider;

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
                    .AddExtensionsFromString(
                        "directive @custom(d: DateTime) on FIELD")
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
                var request = new QueryRequest(@"
                query a($d: DateTime!) {
                    a: extendedScalar(d: ""2018-01-01T01:00:00.000Z"")
                    b: extendedScalar(d: $d)
                        @custom(d: ""2020-09-01T01:00:00.000Z"")
                }");
                request.VariableValues = new Dictionary<string, object>
                {
                    {"d", "2019-01-01T01:00:00.000Z"}
                };
                request.Services = scope.ServiceProvider;

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
                    .AddExtensionsFromString(
                        "directive @custom(d: DateTime) on FIELD")
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
                var request = new QueryRequest(@"
                query a($d: DateTime!) {
                    a: extendedScalar(d: ""2018-01-01T01:00:00.000Z"")
                    b: extendedScalar(d: $d)
                    c: extendedScalar(d: $d)
                        @custom(d: ""2020-09-01T01:00:00.000Z"")
                }");
                request.VariableValues = new Dictionary<string, object>
                {
                    {"d", "2019-01-01T01:00:00.000Z"}
                };
                request.Services = scope.ServiceProvider;

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
                var request = new QueryRequest(@"
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
                    }");
                request.Services = scope.ServiceProvider;

                result = await executor.ExecuteAsync(request);
            }

            // assert
            Snapshot.Match(result);
        }

        [Fact]
        public async Task ConnectionLost()
        {
            // arrange
            var connections = new Dictionary<string, HttpClient>();
            IHttpClientFactory clientFactory = CreateRemoteSchemas(connections);

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton(clientFactory);
            serviceCollection.AddStitchedSchema(builder =>
                builder.AddSchemaFromHttp("contract")
                    .AddSchemaFromHttp("customer")
                    .RenameType("CreateCustomerInput", "CreateCustomerInput2")
                    .AddExtensionsFromString(
                        FileResource.Open("StitchingExtensions.graphql"))
                    .AddSchemaConfiguration(c =>
                        c.RegisterType<PaginationAmountType>())
                    .AddExecutionConfiguration(b =>
                    {
                        b.AddErrorFilter(error =>
                        {
                            if (error.Exception is Exception ex)
                            {
                                return ErrorBuilder.FromError(error)
                                    .ClearExtensions()
                                    .SetMessage(ex.Message)
                                    .SetException(null)
                                    .Build();
                            };
                            return error;
                        });
                    }));

            IServiceProvider services =
                serviceCollection.BuildServiceProvider();

            IQueryExecutor executor = services
                .GetRequiredService<IQueryExecutor>();
            IExecutionResult result = null;

            using (IServiceScope scope = services.CreateScope())
            {
                var request = new QueryRequest(@"
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
                    }");
                request.Services = scope.ServiceProvider;

                result = await executor.ExecuteAsync(request);
            }

            var client = new HttpClient
            {
                BaseAddress = new Uri("http://127.0.0.1")
            }; ;
            connections["contract"] = client;
            connections["customer"] = client;

            // act
            using (IServiceScope scope = services.CreateScope())
            {
                var request = new QueryRequest(@"
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
                    }");
                request.Services = scope.ServiceProvider;

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
                var request = new QueryRequest(@"
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
                    }");
                request.Services = scope.ServiceProvider;

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
                var request = new QueryRequest(@"
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
                    }");
                request.Services = scope.ServiceProvider;

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
                var request = new QueryRequest(@"
                {
                    customer(id: ""Q3VzdG9tZXIteDE="") {
                        contracts {
                            id
                            ... on LifeInsuranceContract {
                                error
                            }
                        }
                    }
                }");
                request.Services = scope.ServiceProvider;

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
                        c.RegisterType<PaginationAmountType>()));

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

        private IHttpClientFactory CreateRemoteSchemas()
        {
            return CreateRemoteSchemas(new Dictionary<string, HttpClient>());
        }

        private IHttpClientFactory CreateRemoteSchemas(
            Dictionary<string, HttpClient> connections)
        {
            TestServer server_contracts = TestServerFactory.Create(
                ContractSchemaFactory.ConfigureSchema,
                ContractSchemaFactory.ConfigureServices,
                new QueryMiddlewareOptions());

            TestServer server_customers = TestServerFactory.Create(
                CustomerSchemaFactory.ConfigureSchema,
                CustomerSchemaFactory.ConfigureServices,
                new QueryMiddlewareOptions());

            connections["contract"] = server_contracts.CreateClient();
            connections["customer"] = server_customers.CreateClient();

            var httpClientFactory = new Mock<IHttpClientFactory>();
            httpClientFactory.Setup(t => t.CreateClient(It.IsAny<string>()))
                .Returns(new Func<string, HttpClient>(n =>
                {
                    if (connections.ContainsKey(n))
                    {
                        return connections[n];
                    }

                    throw new Exception();
                }));
            return httpClientFactory.Object;
        }
    }
}
