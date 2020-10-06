using HotChocolate.Language;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Stitching.Introspection
{
    public class IntrospectionClientTests
    {
        [Fact]
        public void IntrospectionWithSubscription()
        {
            // arrange
            var features = new SchemaFeatures
            {
                HasSubscriptionSupport = true
            };

            // act
            DocumentNode document =
                IntrospectionClient.CreateIntrospectionQuery(features);

            // assert
            QuerySyntaxSerializer.Serialize(document).MatchSnapshot();
        }

        [Fact]
        public void IntrospectionWithoutSubscription()
        {
            // arrange
            var features = new SchemaFeatures
            {
                HasSubscriptionSupport = false
            };

            // act
            DocumentNode document =
                IntrospectionClient.CreateIntrospectionQuery(features);

            // assert
            QuerySyntaxSerializer.Serialize(document).MatchSnapshot();
        }

        [Fact]
        public void IntrospectionWithDirectiveLocationField()
        {
            // arrange
            var features = new SchemaFeatures
            {
                HasDirectiveLocations = true
            };

            // act
            DocumentNode document =
                IntrospectionClient.CreateIntrospectionQuery(features);

            // assert
            QuerySyntaxSerializer.Serialize(document).MatchSnapshot();
        }

        [Fact]
        public void IntrospectionWithoutDirectiveLocationField()
        {
            // arrange
            var features = new SchemaFeatures
            {
                HasDirectiveLocations = false
            };

            // act
            DocumentNode document =
                IntrospectionClient.CreateIntrospectionQuery(features);

            // assert
            QuerySyntaxSerializer.Serialize(document).MatchSnapshot();
        }

        [Fact]
        public void IntrospectionWithDirectiveIsRepeatableField()
        {
            // arrange
            var features = new SchemaFeatures
            {
                HasRepeatableDirectives = true
            };

            // act
            DocumentNode document =
                IntrospectionClient.CreateIntrospectionQuery(features);

            // assert
            QuerySyntaxSerializer.Serialize(document).MatchSnapshot();
        }

        [Fact]
        public void IntrospectionWithoutDirectiveIsRepeatableField()
        {
            // arrange
            var features = new SchemaFeatures
            {
                HasRepeatableDirectives = false
            };

            // act
            DocumentNode document =
                IntrospectionClient.CreateIntrospectionQuery(features);

            // assert
            QuerySyntaxSerializer.Serialize(document).MatchSnapshot();
        }
    }
}
