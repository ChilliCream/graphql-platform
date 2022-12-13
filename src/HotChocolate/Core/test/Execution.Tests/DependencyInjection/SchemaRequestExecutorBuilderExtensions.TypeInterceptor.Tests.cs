using System;
using System.Threading.Tasks;
using HotChocolate.Execution.Configuration;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Snapshooter.Xunit;
using Xunit;
using static HotChocolate.Tests.TestHelper;

namespace HotChocolate.Execution.DependencyInjection;

public class SchemaRequestExecutorBuilderExtensionsTypeInterceptorTests
{
    [Fact]
    public async Task OnBeforeSchemaCreate()
    {
        // arrange
        Snapshot.FullName();
        var invoked = false;

        // act
        await CreateSchemaAsync(
            c => c
                .AddQueryType(
                    d => d
                        .Name("Query")
                        .Field("foo")
                        .Resolve("bar"))
                .OnBeforeSchemaCreate(
                    (_, _) =>
                    {
                        invoked = true;
                    }));

        // assert
        Assert.True(invoked);
    }

    [Fact]
    public void OnBeforeSchemaCreate_Builder_IsNull()
    {
        void Action() =>
            SchemaRequestExecutorBuilderExtensions
                .OnBeforeSchemaCreate(null!, (_, _) => { });

        Assert.Throws<ArgumentNullException>(Action);
    }

    [Fact]
    public void OnBeforeSchemaCreate_OnError_IsNull()
    {
        var builder = new Mock<IRequestExecutorBuilder>();

        void Action() =>
            builder.Object.OnBeforeSchemaCreate(null!);

        Assert.Throws<ArgumentNullException>(Action);
    }

    [Fact]
    public async Task OnAfterSchemaCreate()
    {
        // arrange
        Snapshot.FullName();
        var invoked = false;

        // act
        await CreateSchemaAsync(
            c => c
                .AddQueryType(
                    d => d
                        .Name("Query")
                        .Field("foo")
                        .Resolve("bar"))
                .OnAfterSchemaCreate(
                    (_, _) =>
                    {
                        invoked = true;
                    }));

        // assert
        Assert.True(invoked);
    }

    [Fact]
    public void OnAfterSchemaCreate_Builder_IsNull()
    {
        void Action() =>
            SchemaRequestExecutorBuilderExtensions
                .OnAfterSchemaCreate(null!, (_, _) => { });

        Assert.Throws<ArgumentNullException>(Action);
    }

    [Fact]
    public void OnAfterSchemaCreate_OnError_IsNull()
    {
        var builder = new Mock<IRequestExecutorBuilder>();

        void Action() =>
            builder.Object.OnAfterSchemaCreate(null!);

        Assert.Throws<ArgumentNullException>(Action);
    }

    [Fact]
    public async Task OnSchemaError()
    {
        // arrange
        Snapshot.FullName();
        Exception ex = null;

        // act
        try
        {
            await new ServiceCollection()
                .AddGraphQL()
                .OnSchemaError((_, exception) => ex = exception)
                .BuildSchemaAsync();
        }
        catch
        {
            // ignored
        }

        // assert
        Assert.IsType<SchemaException>(ex);
    }

    [Fact]
    public void OnSchemaError_Builder_IsNull()
    {
        void Action() =>
            SchemaRequestExecutorBuilderExtensions
                .OnSchemaError(null!, (_, _) => { });

        Assert.Throws<ArgumentNullException>(Action);
    }

    [Fact]
    public void OnSchemaError_OnError_IsNull()
    {
        var builder = new Mock<IRequestExecutorBuilder>();

        void Action() =>
            builder.Object.OnSchemaError(null!);

        Assert.Throws<ArgumentNullException>(Action);
    }
}
