using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.TestHost;
using ChilliCream.Testing;
using HotChocolate.AspNetCore;
using HotChocolate.AspNetCore.Tests.Utilities;
using HotChocolate.Language;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Utilities.Introspection
{
    public class IntrospectionDeserializerTests
    {
        [Fact]
        public void DeserializeStarWarsIntrospectionResult()
        {
            // arrange
            string json = FileResource.Open("StarWarsIntrospectionResult.json");
            IntrospectionResult result = JsonSerializer.Deserialize<IntrospectionResult>(
                json,
                IntrospectionClient.SerializerOptions);

            // act
            DocumentNode schema = IntrospectionDeserializer.Deserialize(result);

            // assert
            SchemaSyntaxSerializer.Serialize(schema).MatchSnapshot();
        }

        [Fact]
        public void DeserializeIntrospectionWithIntDefaultValues()
        {
            // arrange
            string json = FileResource.Open("IntrospectionWithDefaultValues.json");
            IntrospectionResult result = JsonSerializer.Deserialize<IntrospectionResult>(
                json,
                IntrospectionClient.SerializerOptions);

            // act
            DocumentNode schema = IntrospectionDeserializer.Deserialize(result);

            // assert
            SchemaSyntaxSerializer.Serialize(schema).MatchSnapshot();
        }
    }

    public class IntrospectionClientTests
        : ServerTestBase
    {
        public IntrospectionClientTests(TestServerFactory serverFactory)
            : base(serverFactory)
        {
        }

        [Fact]
        public async Task GetSchemaFeatures()
        {
            // arrange
            TestServer server = CreateStarWarsServer();
            var introspectionClient = new IntrospectionClient();

            // act
            ISchemaFeatures features =
                await introspectionClient.GetSchemaFeaturesAsync(
                        server.CreateClient());

            // assert
            Assert.True(features.HasDirectiveLocations);
            Assert.True(features.HasRepeatableDirectives);
            Assert.True(features.HasSubscriptionSupport);
        }
    }
}
