using System;
using System.Text.Unicode;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.StarWars;
using HotChocolate.StarWars.Types;
using HotChocolate.Types;

//|             Method |      Mean |    Error |   StdDev |    Median | Rank |      Gen 0 |     Gen 1 |     Gen 2 | Allocated |
//|------------------- |----------:|---------:|---------:|----------:|-----:|-----------:|----------:|----------:|----------:|
//|           StarWars |  38.23 ms | 0.735 ms | 2.001 ms |  38.11 ms |    1 |          - |         - |         - |   4.11 MB |
//| GitHub_SchemaFirst | 187.41 ms | 3.746 ms | 9.737 ms | 188.91 ms |    3 | 11000.0000 | 4000.0000 | 2000.0000 |  46.19 MB |
//|          CodeFirst |  69.26 ms | 1.319 ms | 1.296 ms |  69.23 ms |    2 |  3000.0000 | 1000.0000 |         - |  15.97 MB |

namespace HotChocolate.Execution.Benchmarks
{
    [RPlotExporter, CategoriesColumn, RankColumn, MeanColumn, MedianColumn, MemoryDiagnoser]
    public class SchemaBuildingBenchmark
    {
        [Benchmark]
        public async Task StarWars()
        {
            await new ServiceCollection()
                .AddStarWarsRepositories()
                .AddGraphQL()
                .AddStarWarsTypes()
                .BuildSchemaAsync();
        }

        [Benchmark]
        public void GitHub_SchemaFirst()
        {
            try
            {
                SchemaBuilder.New()
                    .AddDocumentFromString(new ResourceHelper().GetResourceString("GitHub.graphql"))
                    .Use(next => context => next(context))
                    .ModifyOptions(x => x.StrictValidation = false)
                    .Create();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        [Benchmark]
        public void CodeFirst()
        {
            try
            {
                SchemaBuilder.New()
                    .AddQueryType(new ObjectType(x =>
                    {
                        x.Name("Query");
                        for (var i = 0; i < 20; i++)
                        {
                            x.Field("QueryField" + Guid.NewGuid().ToString("N"))
                                .Resolve("")
                                .Type(new ObjectType(y =>
                                {
                                    y.Name("Foo" + Guid.NewGuid().ToString("N"));
                                    for (var j = 0; j < 20; j++)
                                    {
                                        y.Field("FooField" + j)
                                            .Type<StringType>()
                                            .Resolve("")
                                            .Argument("in",
                                                z =>
                                                {
                                                    z.Type(new InputObjectType(e =>
                                                    {
                                                        e.Name($"Bar_{Guid.NewGuid():N}");
                                                        for (var m = 0; m < 10; m++)
                                                        {
                                                            e.Field("BarField" + m)
                                                                .Type<StringType>();
                                                        }
                                                    }));
                                                });
                                    }
                                }));
                        }
                    }))
                    .Create();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
