using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using HotChocolate.ConferencePlanner;
using HotChocolate.Execution;

namespace HotChocolate.Benchmarks
{
    [RPlotExporter, CategoriesColumn, RankColumn, MeanColumn, MedianColumn, MemoryDiagnoser]
    public class QueryBenchmarks : BenchmarkBase
    {
        [Benchmark]
        public async Task Sessions_TitleAndAbstract_10_Items() =>
            await BenchmarkAsync(@"
                {
                    sessions(first: 10) {
                        nodes {
                            title
                            abstract
                        }
                    }
                }
            ");

        public async Task Print_Sessions_TitleAndAbstract_10_Items() =>
            await PrintQueryPlanAsync(@"
                {
                    sessions(first: 10) {
                        nodes {
                            title
                            abstract
                        }
                    }
                }
            ");

        [Benchmark]
        public async Task Sessions_TitleAndAbstractAndTrackName_10_Items() =>
            await BenchmarkAsync(@"
                {
                    sessions(first: 10) {
                        nodes {
                            title
                            abstract
                            track {
                                name
                            }
                        }
                    }
                }
            ");

        public async Task Print_Sessions_TitleAndAbstractAndTrackName_10_Items() =>
            await PrintQueryPlanAsync(@"
                {
                    sessions(first: 10) {
                        nodes {
                            title
                            abstract
                            track {
                                name
                            }
                        }
                    }
                }
            ");

        [Benchmark]
        public async Task Sessions_Medium() =>
            await BenchmarkAsync(SessionMediumQuery);

        public async Task Print_Sessions_Medium() =>
            await PrintQueryPlanAsync(SessionMediumQuery);

        public string SessionMediumQuery { get; } = Resources.SessionMediumQuery;

        [Benchmark]
        public async Task Sessions_Large() =>
            await BenchmarkAsync(SessionLargeQuery);

        public async Task Print_Sessions_Large() =>
            await PrintQueryPlanAsync(SessionLargeQuery);

        public string SessionLargeQuery { get; } = Resources.SessionLargeQuery;

        [Benchmark]
        public async Task Sessions_DataLoader_Large() =>
            await BenchmarkAsync(SessionLargeDLQuery);

        public async Task Print_Sessions_DataLoader_Large() =>
            await PrintQueryPlanAsync(SessionLargeDLQuery);

        public string SessionLargeDLQuery { get; } = Resources.SessionLargeDLQuery;
    }
}
