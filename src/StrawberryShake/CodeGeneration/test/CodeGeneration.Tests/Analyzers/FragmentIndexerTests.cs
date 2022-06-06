using System.Linq;
using System.Threading.Tasks;
using ChilliCream.Testing;
using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace StrawberryShake.CodeGeneration.Analyzers;

public class FragmentIndexerTests
{
    [Fact]
    public async Task All_Fragments_Have_No_Dependency()
    {
        // arrange
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddDocumentFromString(FileResource.Open("CoinApi.graphql"))
                .AddType<UploadType>()
                .UseField(_ => _ => default)
                .BuildSchemaAsync();

        DocumentNode document = Utf8GraphQLParser.Parse(
            @"fragment AssetList_List on AssetsConnection {
                nodes {
                    name
                    price {
                        lastPrice
                    }
                }
            }

            fragment Index_Symbols on AssetsConnection {
                nodes {
                    symbol
                }
            }

            fragment NavigationControls_Page on PageInfo {
                hasNextPage
                endCursor
            }

            query GetAssets($after: String = null) {
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
            }");

        // act
        var index = FragmentIndexer.Index(schema, document);

        // assert
        Assert.Collection(
            index.Values,
            t => Assert.Empty(t.DependsOn),
            t => Assert.Empty(t.DependsOn),
            t => Assert.Empty(t.DependsOn));
    }

    [Fact]
    public async Task Fragments_With_MultiLevel_Dependency()
    {
        // arrange
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddDocumentFromString(FileResource.Open("CoinApi.graphql"))
                .AddType<UploadType>()
                .UseField(_ => _ => default)
                .BuildSchemaAsync();

        DocumentNode document = Utf8GraphQLParser.Parse(
            @"fragment AssetList_List on AssetsConnection {
                nodes {
                    name
                    price {
                        lastPrice
                    }
                }
            }

            fragment Index_Symbols on AssetsConnection {
                nodes {
                    symbol
                }
            }

            fragment NavigationControls_Page on PageInfo {
                hasNextPage
                endCursor
            }

            fragment _Connection on AssetsConnection {
                ... AssetList_List
                ... Index_Symbols
                pageInfo {
                    ... NavigationControls_Page
                }
            }

            query GetAssets($after: String = null) {
                assets(after: $after order: { price: { marketCap: DESC } }) {
                    ... _Connection
                }
            }

            subscription OnPriceChange {
                onPriceChange {
                    lastPrice
                }
            }");

        // act
        var index = FragmentIndexer.Index(schema, document);

        // assert
        Assert.Collection(
            index.Values.OrderBy(t => t.Name),
            t =>
            {
                Assert.Collection(
                    t.DependsOn.OrderBy(d => d),
                    d => Assert.Equal("AssetList_List", d),
                    d => Assert.Equal("Index_Symbols", d),
                    d => Assert.Equal("NavigationControls_Page", d));

                Assert.Collection(
                    t.Siblings.OrderBy(d => d),
                    d => Assert.Equal("AssetList_List", d),
                    d => Assert.Equal("Index_Symbols", d));
            },
            t => Assert.Empty(t.DependsOn),
            t => Assert.Empty(t.DependsOn),
            t => Assert.Empty(t.DependsOn));
    }

    [Fact]
    public async Task FragmentName_Not_Unique()
    {
        // arrange
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddDocumentFromString(FileResource.Open("CoinApi.graphql"))
                .AddType<UploadType>()
                .UseField(_ => _ => default)
                .BuildSchemaAsync();

        DocumentNode document = Utf8GraphQLParser.Parse(
            @"fragment AssetList_List on AssetsConnection {
                abc
            }

            fragment AssetList_List on AssetsConnection {
                def
            }");

        // act
        void Error() => FragmentIndexer.Index(schema, document);

        // assert
        Assert.Contains("AssetList_List", Assert.Throws<GraphQLException>(Error).Message);
    }

    [Fact]
    public async Task FragmentName_Not_Found()
    {
        // arrange
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddDocumentFromString(FileResource.Open("CoinApi.graphql"))
                .AddType<UploadType>()
                .UseField(_ => _ => default)
                .BuildSchemaAsync();

        DocumentNode document = Utf8GraphQLParser.Parse(
            @"fragment AssetList_List on AssetsConnection {
                ... Foo
            }");

        // act
        void Error() => FragmentIndexer.Index(schema, document);

        // assert
        Assert.Contains("Foo", Assert.Throws<GraphQLException>(Error).Message);
    }
}
