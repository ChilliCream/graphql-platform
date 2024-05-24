using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Execution.Instrumentation;
using FileResource = ChilliCream.Testing.FileResource;

namespace HotChocolate.Execution.Pipeline;

public class OperationCacheMiddlewareTests
{
    [Fact]
    public async Task Ensure_Cache_Is_Hit_When_Two_Ops_In_Request()
    {
        // arrange
        const string requestDocument =
            """
            query GetBazBar {
                bazOrBar {
                    ... on Foo {
                        ... on Foo {
                            field
                            ... on Bar {
                                baz {
                                    foo {
                                        field
                                    }
                                }
                            }
                        }
                    }
                    ... on Bar {
                        baz {
                            foo {
                                field
                            }
                        }
                    }
                }
            }

            query FooBar {
                bazOrBar {
                    __typename
                }
            }
            """;

        var request = OperationRequest.FromSourceText(requestDocument);

        var diagnostics = new CacheHit();

        var executor =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddDocumentFromString(FileResource.Open("CostSchema.graphql"))
                .UseField(_ => _ => default)
                .AddDiagnosticEventListener(_ => diagnostics)
                .UseDefaultPipeline()
                .BuildRequestExecutorAsync();

        // act
        await executor.ExecuteAsync(request.WithOperationName("GetBazBar"));
        await executor.ExecuteAsync(request.WithOperationName("FooBar"));
        await executor.ExecuteAsync(request.WithOperationName("GetBazBar"));
        await executor.ExecuteAsync(request.WithOperationName("FooBar"));
        await executor.ExecuteAsync(request.WithOperationName("GetBazBar"));
        await executor.ExecuteAsync(request.WithOperationName("GetBazBar"));
        await executor.ExecuteAsync(request.WithOperationName("GetBazBar"));
        await executor.ExecuteAsync(request.WithOperationName("FooBar"));

        // assert
        Assert.Equal(2, diagnostics.AddedToCache);
        Assert.Equal(2, diagnostics.Compiled);
        Assert.Equal(6, diagnostics.RetrievedFromCache);
    }

     [Fact]
    public async Task Ensure_Cache_Is_Hit_When_Single_Op()
    {
        // arrange
        const string requestDocument =
            """
            query GetBazBar {
                bazOrBar {
                    ... on Foo {
                        ... on Foo {
                            field
                            ... on Bar {
                                baz {
                                    foo {
                                        field
                                    }
                                }
                            }
                        }
                    }
                    ... on Bar {
                        baz {
                            foo {
                                field
                            }
                        }
                    }
                }
            }
            """;

        var request = OperationRequest.FromSourceText(requestDocument);

        var diagnostics = new CacheHit();

        var executor =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddDocumentFromString(FileResource.Open("CostSchema.graphql"))
                .UseField(_ => _ => default)
                .UseDefaultPipeline()
                .AddDiagnosticEventListener(_ => diagnostics)
                .BuildRequestExecutorAsync();

        // act
        await executor.ExecuteAsync(request);
        await executor.ExecuteAsync(request);

        // assert
        Assert.Equal(1, diagnostics.AddedToCache);
        Assert.Equal(1, diagnostics.Compiled);
        Assert.Equal(1, diagnostics.RetrievedFromCache);
    }

    public sealed class CacheHit : ExecutionDiagnosticEventListener
    {
        public int RetrievedFromCache { get; private set; }

        public int AddedToCache { get; private set; }

        public int Compiled { get; private set; }

        public override void RetrievedOperationFromCache(IRequestContext context)
        {
            RetrievedFromCache++;
        }

        public override void AddedOperationToCache(IRequestContext context)
        {
            AddedToCache++;
        }

        public override IDisposable CompileOperation(IRequestContext context)
        {
            Compiled++;
            return base.CompileOperation(context);
        }
    }
}
