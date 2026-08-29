using System.Collections.Immutable;
using HotChocolate.Types.Analyzers.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HotChocolate.Types;

public class ServiceAttributeInfoTests
{
    [Fact]
    public void GetServiceAttributeInfo_Should_ReturnDirectServiceKey()
    {
        // arrange
        var compilation = TestHelper.CreateCompilation(
            """
            using HotChocolate;

            class Query
            {
                public void Execute([Service("service-key")] object service)
                {
                }
            }
            """);

        // act
        var info = GetParameter(compilation).GetServiceAttributeInfo(compilation);

        // assert
        Assert.True(info.HasServiceAttribute);
        Assert.Equal("service-key", info.ServiceKey?.Value);
        Assert.False(info.IsServiceKeyUndeterminable);
        Assert.False(info.HasFromKeyedServicesAttribute);
    }

    [Fact]
    public void GetServiceAttributeInfo_Should_ReturnKeyFromSourceDerivedServiceAttribute()
    {
        // arrange
        var compilation = TestHelper.CreateCompilation(
            """
            using HotChocolate;

            class MemoizedInScopeAttribute() : ServiceAttribute("MEMOIZED")
            {
            }

            class Query
            {
                public void Execute([MemoizedInScope] object service)
                {
                }
            }
            """);

        // act
        var info = GetParameter(compilation).GetServiceAttributeInfo(compilation);

        // assert
        Assert.True(info.HasServiceAttribute);
        Assert.Equal("MEMOIZED", info.SourceDerivedServiceKey);
        Assert.False(info.IsServiceKeyUndeterminable);
    }

    [Fact]
    public void GetServiceAttributeInfo_Should_ReturnAnyFromKeyedServicesConstant()
    {
        // arrange
        var compilation = TestHelper.CreateCompilation(
            """
            using Microsoft.Extensions.DependencyInjection;

            enum ServiceKey
            {
                First
            }

            class Query
            {
                public void Execute(
                    [FromKeyedServices("string")] object stringService,
                    [FromKeyedServices(ServiceKey.First)] object enumService,
                    [FromKeyedServices(42)] object integerService)
                {
                }
            }
            """);

        // act
        var parameters = GetParameters(compilation)
            .Select(t => t.GetServiceAttributeInfo(compilation))
            .ToArray();

        // assert
        Assert.Collection(
            parameters,
            t => Assert.Equal("string", t.FromKeyedServicesKey?.Value),
            t => Assert.Equal(0, t.FromKeyedServicesKey?.Value),
            t => Assert.Equal(42, t.FromKeyedServicesKey?.Value));
    }

    [Fact]
    public void GetServiceAttributeInfo_Should_ReportMetadataDerivedServiceKeyAsUndeterminable()
    {
        // arrange
        var reference = TestHelper.CreateReference(
            """
            using HotChocolate;

            public class MetadataServiceAttribute() : ServiceAttribute("metadata")
            {
            }
            """,
            "MetadataAttributes");
        var compilation = (CSharpCompilation)TestHelper.CreateCompilation(
            """
            class Query
            {
                public void Execute([MetadataService] object service)
                {
                }
            }
            """).AddReferences(reference);

        // act
        var info = GetParameter(compilation).GetServiceAttributeInfo(compilation);

        // assert
        Assert.True(info.HasServiceAttribute);
        Assert.Null(info.ServiceKey);
        Assert.True(info.IsServiceKeyUndeterminable);
    }

    [Fact]
    public void GetServiceAttributeInfo_Should_ExposeBothKeyedServiceAttributes()
    {
        // arrange
        var compilation = TestHelper.CreateCompilation(
            """
            using HotChocolate;
            using Microsoft.Extensions.DependencyInjection;

            class Query
            {
                public void Execute(
                    [Service("hot-chocolate")]
                    [FromKeyedServices("dependency-injection")]
                    object service)
                {
                }
            }
            """);

        // act
        var info = GetParameter(compilation).GetServiceAttributeInfo(compilation);

        // assert
        Assert.True(info.HasServiceAttribute);
        Assert.Equal("hot-chocolate", info.ServiceKey?.Value);
        Assert.True(info.HasFromKeyedServicesAttribute);
        Assert.Equal("dependency-injection", info.FromKeyedServicesKey?.Value);
    }

    [Fact]
    public void GetServiceAttributeInfo_Should_ReturnKeylessServiceAttribute()
    {
        // arrange
        var compilation = TestHelper.CreateCompilation(
            """
            using HotChocolate;

            class Query
            {
                public void Execute([Service] object service)
                {
                }
            }
            """);

        // act
        var info = GetParameter(compilation).GetServiceAttributeInfo(compilation);

        // assert
        Assert.True(info.HasServiceAttribute);
        Assert.Null(info.ServiceKey);
        Assert.False(info.IsServiceKeyUndeterminable);
    }

    private static IParameterSymbol GetParameter(CSharpCompilation compilation)
        => GetParameters(compilation).Single();

    private static ImmutableArray<IParameterSymbol> GetParameters(CSharpCompilation compilation)
    {
        var syntaxTree = compilation.SyntaxTrees.Single();
        var method = syntaxTree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single();
        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        return ((IMethodSymbol)semanticModel.GetDeclaredSymbol(method)!).Parameters;
    }
}
