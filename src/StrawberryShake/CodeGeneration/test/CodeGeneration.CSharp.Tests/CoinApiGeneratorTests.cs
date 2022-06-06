using ChilliCream.Testing;
using Xunit;
using static StrawberryShake.CodeGeneration.CSharp.GeneratorTestHelper;

namespace StrawberryShake.CodeGeneration.CSharp;

public class CoinApiGeneratorTests
{
    [Fact]
    public void Multiple_Fragments_On_SameLevel()
    {
        AssertResult(
            @"fragment AssetList_List on AssetsConnection {
                nodes {
                    name
                    price {
                        lastPrice
                    }
                }
            }",
            @"fragment Index_Symbols on AssetsConnection {
                nodes {
                    symbol
                }
            }",
            @"query GetAssets($after: String = null) {
                assets(after: $after order: { price: { marketCap: DESC } }) {
                    ... AssetList_List
                    ... Index_Symbols
                    pageInfo {
                        ... NavigationControls_Page
                    }
                }
            }

            subscription OnPriceChange {
                onPriceChange {
                    lastPrice
                }
            }",
            @"fragment NavigationControls_Page on PageInfo {
                hasNextPage
                endCursor
            }",
            FileResource.Open("CoinApi.graphql"),
            FileResource.Open("Schema.extensions.graphql"));
    }

     [Fact]
    public void Unused_Fragment()
    {
        AssertResult(
            @"fragment AssetList_List on AssetsConnection {
                nodes {
                    name
                    price {
                        lastPrice
                    }
                }
            }",
            @"fragment Index_Symbols on AssetsConnection {
                nodes {
                    symbol
                }
            }",
            @"query GetAssets($after: String = null) {
                assets(after: $after order: { price: { marketCap: DESC } }) {
                    ... AssetList_List
                    pageInfo {
                        ... NavigationControls_Page
                    }
                }
            }

            subscription OnPriceChange {
                onPriceChange {
                    lastPrice
                }
            }",
            @"fragment NavigationControls_Page on PageInfo {
                hasNextPage
                endCursor
            }",
            FileResource.Open("CoinApi.graphql"),
            FileResource.Open("Schema.extensions.graphql"));
    }
}
