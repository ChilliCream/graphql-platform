using System;
using System.Collections.Generic;
using System.Net.Http;
using Xunit;
using HotChocolate.AspNetCore;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Execution;
using Snapshooter.Xunit;
using Moq;
using Microsoft.AspNetCore.TestHost;
using Snapshooter;
using HotChocolate.AspNetCore.Tests.Utilities;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Stitching
{
    public class CustomScalarTypeTests
        : StitchingTestBase
    {
        public CustomScalarTypeTests(TestServerFactory testServerFactory)
            : base(testServerFactory)
        {
        }

        [Fact]
        public async Task AllowCustomScalarTypes()
        {
            // arrange
            var serviceCollection = new ServiceCollection();

            var connections = new Dictionary<string, HttpClient>();
            serviceCollection.AddSingleton(CreateRemoteSchemas(connections));

            serviceCollection.AddStitchedSchema(builder => builder
                .AddSchemaFromHttp("server_1"));

            IServiceProvider services =
                serviceCollection.BuildServiceProvider();

            IQueryExecutor executor = services
                .GetRequiredService<IQueryExecutor>();

            // act
            IExecutionResult result = null;

            using (IServiceScope scope = services.CreateScope())
            {
                IReadOnlyQueryRequest request = QueryRequestBuilder.New()
                    .SetQuery("{ oneMinute }")
                    .SetServices(scope.ServiceProvider)
                    .Create();

                result = await executor.ExecuteAsync(request);
            }

            // assert
            result.MatchSnapshot(new SnapshotNameExtension("result"));
            executor.Schema.ToString().MatchSnapshot(
                new SnapshotNameExtension("schema"));
        }
        protected override IHttpClientFactory CreateRemoteSchemas(
               Dictionary<string, HttpClient> connections)
        {
            TestServer server_1 = TestServerFactory.Create(
                services => services.AddGraphQL(
                    SchemaBuilder.New()
                        .AddDocumentFromString
                        (
                            @"
                            type Query { oneMinute: TimeSpan }
                            "
                        )
                        .AddResolver("Query", "oneMinute", ctx => TimeSpan.FromMinutes(1))
                        .AddType<TimeSpanType>()
                        .Create()),
                app => app.UseGraphQL());

            connections["server_1"] = server_1.CreateClient();

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

        public class TimeSpanType : ScalarType<TimeSpan, StringValueNode>
        {
            public TimeSpanType()
                : base("TimeSpan")
            {
            }

            protected override TimeSpan ParseLiteral(StringValueNode literal)
            {
                return TimeSpan.ParseExact(literal.Value, "c", null);
            }

            protected override StringValueNode ParseValue(TimeSpan value)
            {
                return new StringValueNode(value.ToString("c"));
            }

            public override bool TrySerialize(object value, out object? serialized)
            {
                if (value is null)
                {
                    serialized = null;
                    return true;
                }

                if (value is TimeSpan t)
                {
                    serialized = t.ToString("c");
                    return true;
                }

                serialized = null;
                return false;
            }

            public override bool TryDeserialize(object serialized, out object? value)
            {
                if (serialized is null)
                {
                    value = null;
                    return true;
                }

                if (serialized is string s && TimeSpan.TryParseExact(s, "c", null, out TimeSpan t))
                {
                    value = t;
                    return true;
                }

                value = null;
                return false;
            }
        }
    }
}
