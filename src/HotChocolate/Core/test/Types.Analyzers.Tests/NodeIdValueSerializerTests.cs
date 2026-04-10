namespace HotChocolate.Types;

public class NodeIdValueSerializerTests
{
    [Fact]
    public async Task Generate_Serializer_For_Record_Struct_With_Two_Int_Properties()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            [
                """
                using HotChocolate.Execution.Configuration;
                using Microsoft.Extensions.DependencyInjection;

                namespace TestNamespace;

                public readonly record struct FooId(int A, int B);

                public static class Setup
                {
                    public static IRequestExecutorBuilder Configure(IRequestExecutorBuilder builder)
                        => builder.AddNodeIdValueSerializerFrom<FooId>();
                }
                """
            ],
            enableInterceptors: true).MatchMarkdownAsync();
    }

    [Fact]
    public async Task Generate_Serializer_For_Record_Struct_With_String_And_Int()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            [
                """
                using HotChocolate.Execution.Configuration;
                using Microsoft.Extensions.DependencyInjection;

                namespace TestNamespace;

                public readonly record struct OrderId(string Tenant, int Id);

                public static class Setup
                {
                    public static IRequestExecutorBuilder Configure(IRequestExecutorBuilder builder)
                        => builder.AddNodeIdValueSerializerFrom<OrderId>();
                }
                """
            ],
            enableInterceptors: true).MatchMarkdownAsync();
    }

    [Fact]
    public async Task Generate_Serializer_For_Record_Struct_With_Guid()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            [
                """
                using System;
                using HotChocolate.Execution.Configuration;
                using Microsoft.Extensions.DependencyInjection;

                namespace TestNamespace;

                public readonly record struct EntityId(string Type, Guid Id);

                public static class Setup
                {
                    public static IRequestExecutorBuilder Configure(IRequestExecutorBuilder builder)
                        => builder.AddNodeIdValueSerializerFrom<EntityId>();
                }
                """
            ],
            enableInterceptors: true).MatchMarkdownAsync();
    }

    [Fact]
    public async Task Generate_Serializer_For_Record_Struct_With_Single_Int()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            [
                """
                using HotChocolate.Execution.Configuration;
                using Microsoft.Extensions.DependencyInjection;

                namespace TestNamespace;

                public readonly record struct ProductId(int Value);

                public static class Setup
                {
                    public static IRequestExecutorBuilder Configure(IRequestExecutorBuilder builder)
                        => builder.AddNodeIdValueSerializerFrom<ProductId>();
                }
                """
            ],
            enableInterceptors: true).MatchMarkdownAsync();
    }

    [Fact]
    public async Task Generate_Serializer_For_Record_Struct_With_Three_Parts()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            [
                """
                using HotChocolate.Execution.Configuration;
                using Microsoft.Extensions.DependencyInjection;

                namespace TestNamespace;

                public readonly record struct CompositeKey(string Tenant, int Category, long Id);

                public static class Setup
                {
                    public static IRequestExecutorBuilder Configure(IRequestExecutorBuilder builder)
                        => builder.AddNodeIdValueSerializerFrom<CompositeKey>();
                }
                """
            ],
            enableInterceptors: true).MatchMarkdownAsync();
    }

    [Fact]
    public async Task Generate_Serializer_For_Record_Struct_With_Bool()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            [
                """
                using HotChocolate.Execution.Configuration;
                using Microsoft.Extensions.DependencyInjection;

                namespace TestNamespace;

                public readonly record struct FeatureFlag(string Name, bool Enabled);

                public static class Setup
                {
                    public static IRequestExecutorBuilder Configure(IRequestExecutorBuilder builder)
                        => builder.AddNodeIdValueSerializerFrom<FeatureFlag>();
                }
                """
            ],
            enableInterceptors: true).MatchMarkdownAsync();
    }

    [Fact]
    public async Task Generate_Serializer_For_Plain_Struct_With_Settable_Properties()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            [
                """
                using HotChocolate.Execution.Configuration;
                using Microsoft.Extensions.DependencyInjection;

                namespace TestNamespace;

                public struct LegacyId
                {
                    public string Region { get; set; }
                    public int Code { get; set; }
                }

                public static class Setup
                {
                    public static IRequestExecutorBuilder Configure(IRequestExecutorBuilder builder)
                        => builder.AddNodeIdValueSerializerFrom<LegacyId>();
                }
                """
            ],
            enableInterceptors: true).MatchMarkdownAsync();
    }

    [Fact]
    public async Task Generate_Multiple_Serializers()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            [
                """
                using HotChocolate.Execution.Configuration;
                using Microsoft.Extensions.DependencyInjection;

                namespace TestNamespace;

                public readonly record struct BrandId(int Value);
                public readonly record struct ProductId(string Sku, int BrandId);

                public static class Setup
                {
                    public static IRequestExecutorBuilder Configure(IRequestExecutorBuilder builder)
                        => builder
                            .AddNodeIdValueSerializerFrom<BrandId>()
                            .AddNodeIdValueSerializerFrom<ProductId>();
                }
                """
            ],
            enableInterceptors: true).MatchMarkdownAsync();
    }

    [Fact]
    public async Task Generate_Serializer_For_Record_Struct_With_Short()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            [
                """
                using HotChocolate.Execution.Configuration;
                using Microsoft.Extensions.DependencyInjection;

                namespace TestNamespace;

                public readonly record struct SmallId(short Value);

                public static class Setup
                {
                    public static IRequestExecutorBuilder Configure(IRequestExecutorBuilder builder)
                        => builder.AddNodeIdValueSerializerFrom<SmallId>();
                }
                """
            ],
            enableInterceptors: true).MatchMarkdownAsync();
    }

    [Fact]
    public async Task Generate_Serializer_For_Record_Struct_With_Long()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            [
                """
                using HotChocolate.Execution.Configuration;
                using Microsoft.Extensions.DependencyInjection;

                namespace TestNamespace;

                public readonly record struct BigId(long Value);

                public static class Setup
                {
                    public static IRequestExecutorBuilder Configure(IRequestExecutorBuilder builder)
                        => builder.AddNodeIdValueSerializerFrom<BigId>();
                }
                """
            ],
            enableInterceptors: true).MatchMarkdownAsync();
    }
}
