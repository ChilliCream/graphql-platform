using ChilliCream.Testing;
using Xunit;
using static StrawberryShake.CodeGeneration.CSharp.GeneratorTestHelper;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public class SchemaGeneratorTests
    {
        [Fact]
        public void Schema_With_Spec_Errors()
        {
            AssertResult(
                strictValidation: false,
                @"
                    query getListingsCount {
                        listings {
                        ... ListingsPayload
                        }
                    }
                    fragment ListingsPayload on ListingsPayload{
                        count
                    }
                ",
                FileResource.Open("BridgeClientDemo.graphql"),
                "extend schema @key(fields: \"id\")");
        }

        [Fact]
        public void Query_With_Nested_Fragments()
        {
            AssertResult(
                strictValidation: true,
                @"
                    query getAll(){
                        listings{
                            ... ListingsPayload
                        }
                    }
                    fragment ListingsPayload on ListingsPayload{
                        items{
                            ... HasListingId
                            ... Offer
                            ... Auction
                        }
                    }
                    fragment HasListingId on Listing{
                        listingId
                    }
                    fragment Offer on Offer{
                        price
                    }
                    fragment Auction on Auction{
                        startingPrice
                    }
                ",
                FileResource.Open("MultipleInterfaceSchema.graphql"),
                "extend schema @key(fields: \"id\")");
        }
    }
}
