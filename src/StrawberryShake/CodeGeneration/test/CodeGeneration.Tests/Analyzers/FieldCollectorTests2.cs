using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChilliCream.Testing;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.StarWars;
using HotChocolate.Types;
using Snapshooter.Xunit;
using StrawberryShake.CodeGeneration.Analyzers.Models;
using Xunit;

namespace StrawberryShake.CodeGeneration.Analyzers;

public class FieldCollectorTests2
{
    [Fact]
    public async Task Collect_First_Level_No_Fragments()
    {
        // arrange
        var schema =
            await new ServiceCollection()
                .AddStarWarsRepositories()
                .AddGraphQL()
                .AddStarWarsTypes()
                .BuildSchemaAsync();

        var document = Utf8GraphQLParser.Parse(
            @"query GetHero {
                hero(episode: NEW_HOPE) {
                    name
                    ... Droid
                }
            }

            fragment Droid on Droid {
                primaryFunction
            }");

        var fragmentIndex = FragmentIndexer.Index(schema, document);
        var fragmentCache = new Dictionary<string, Fragment>();
        var collector = new FieldCollector2(schema, fragmentIndex, fragmentCache);
        var context = new DocumentAnalyzerContext(schema, document);

        // act
        var fragmentTypeModels = collector.CollectFragments(context);

        // assert
        Format(fragmentTypeModels).MatchSnapshot();
    }

    [Fact]
    public async Task Collect_Multiple_Fragments_Single_Level()
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

        var fragmentIndex = FragmentIndexer.Index(schema, document);
        var fragmentCache = new Dictionary<string, Fragment>();
        var collector = new FieldCollector2(schema, fragmentIndex, fragmentCache);
        var context = new DocumentAnalyzerContext(schema, document);

        // act
        var fragmentTypeModels = collector.CollectFragments(context);

        // assert
        Format(fragmentTypeModels).MatchSnapshot();
    }

    private string Format(IEnumerable<OutputTypeModel> typeModels)
    {
        var output = new StringBuilder();
        var queue = new Queue<OutputTypeModel>(typeModels);
        var processed = new HashSet<OutputTypeModel>();

        while (queue.TryDequeue(out OutputTypeModel? current))
        {
            if (processed.Add(current))
            {
                Format(current, output);
            }

            foreach (OutputTypeModel type in current.Implements)
            {
                queue.Enqueue(type);
            }
        }

        return output.ToString();
    }

    private void Format(OutputTypeModel typeModel, StringBuilder output)
    {
        output.Append("public ");
        output.Append(typeModel.IsInterface ? "interface" : "class");
        output.Append(" ");
        output.Append(typeModel.Name);
        output.Append('\n');

        if (typeModel.Implements.Count > 0)
        {
            output.Append("    : ");
            output.Append(string.Join("\n    ,", typeModel.Implements.Select(t => t.Name)));
            output.Append('\n');
        }

        output.Append('{');
        output.Append('\n');

        foreach (OutputFieldModel field in typeModel.Fields)
        {
            output.Append("    // ");
            output.Append(field.Path);
            output.Append('\n');
            output.Append("    public ");
            output.Append(field.Type.Print());
            output.Append(' ');
            output.Append(field.Name);
            output.Append(" { get; set; }");
            output.Append('\n');
        }

        output.Append('}');
        output.Append('\n');
    }
}
