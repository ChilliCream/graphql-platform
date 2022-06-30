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
    public async Task OnBeforeRegisterDependencies()
    {
        // arrange
        Snapshot.FullName();
        var found = false;

        // act
        await CreateSchemaAsync(c => c
            .AddQueryType(d => d
                .Name("Query")
                .Field("foo")
                .Resolve("bar"))
            .OnBeforeRegisterDependencies(
                (_, def, _) =>
                {
                    if (def is not null && def.Name.EqualsOrdinal("Query"))
                    {
                        found = true;
                    }
                }));

        // assert
        Assert.True(found);
    }

    [Fact]
    public async Task OnBeforeRegisterDependencies_Generic()
    {
        // arrange
        Snapshot.FullName();
        var found = false;

        // act
        await CreateSchemaAsync(c => c
            .AddQueryType(d => d
                .Name("Query")
                .Field("foo")
                .Resolve("bar"))
            .OnBeforeRegisterDependencies<ObjectTypeDefinition>(
                (_, def, _) =>
                {
                    if (def is not null && def.Name.EqualsOrdinal("Query"))
                    {
                        found = true;
                    }
                }));

        // assert
        Assert.True(found);
    }

    [Fact]
    public async Task OnBeforeRegisterDependencies_CanHandle()
    {
        // arrange
        Snapshot.FullName();
        var found = false;
        var canHandleInvoked = false;

        // act
        await CreateSchemaAsync(c => c
            .AddQueryType(d => d
                .Name("Query")
                .Field("foo")
                .Resolve("bar"))
            .OnBeforeRegisterDependencies(
                (_, def, _) =>
                {
                    if (def is not null && def.Name.EqualsOrdinal("Query"))
                    {
                        found = true;
                    }
                },
                _ => canHandleInvoked = true));

        // assert
        Assert.True(found);
        Assert.True(canHandleInvoked);
    }

    [Fact]
    public async Task OnBeforeRegisterDependencies_Generic_CanHandle()
    {
        // arrange
        Snapshot.FullName();
        var found = false;
        var canHandleInvoked = false;

        // act
        await CreateSchemaAsync(c => c
            .AddQueryType(d => d
                .Name("Query")
                .Field("foo")
                .Resolve("bar"))
            .OnBeforeRegisterDependencies<ObjectTypeDefinition>(
                (_, def, _) =>
                {
                    if (def is not null && def.Name.EqualsOrdinal("Query"))
                    {
                        found = true;
                    }
                },
                _ => canHandleInvoked = true));

        // assert
        Assert.True(found);
        Assert.True(canHandleInvoked);
    }

    [Fact]
    public async Task OnAfterRegisterDependencies()
    {
        // arrange
        Snapshot.FullName();
        var found = false;

        // act
        await CreateSchemaAsync(c => c
            .AddQueryType(d => d
                .Name("Query")
                .Field("foo")
                .Resolve("bar"))
            .OnAfterRegisterDependencies(
                (_, def, _) =>
                {
                    if (def is not null && def.Name.EqualsOrdinal("Query"))
                    {
                        found = true;
                    }
                }));

        // assert
        Assert.True(found);
    }

    [Fact]
    public async Task OnAfterRegisterDependencies_Generic()
    {
        // arrange
        Snapshot.FullName();
        var found = false;

        // act
        await CreateSchemaAsync(c => c
            .AddQueryType(d => d
                .Name("Query")
                .Field("foo")
                .Resolve("bar"))
            .OnAfterRegisterDependencies<ObjectTypeDefinition>(
                (_, def, _) =>
                {
                    if (def is not null && def.Name.EqualsOrdinal("Query"))
                    {
                        found = true;
                    }
                }));

        // assert
        Assert.True(found);
    }

    [Fact]
    public async Task OnAfterRegisterDependencies_CanHandle()
    {
        // arrange
        Snapshot.FullName();
        var found = false;
        var canHandleInvoked = false;

        // act
        await CreateSchemaAsync(c => c
            .AddQueryType(d => d
                .Name("Query")
                .Field("foo")
                .Resolve("bar"))
            .OnAfterRegisterDependencies(
                (_, def, _) =>
                {
                    if (def is not null && def.Name.EqualsOrdinal("Query"))
                    {
                        found = true;
                    }
                },
                _ => canHandleInvoked = true));

        // assert
        Assert.True(found);
        Assert.True(canHandleInvoked);
    }

    [Fact]
    public async Task OnAfterRegisterDependencies_Generic_CanHandle()
    {
        // arrange
        Snapshot.FullName();
        var found = false;
        var canHandleInvoked = false;

        // act
        await CreateSchemaAsync(c => c
            .AddQueryType(d => d
                .Name("Query")
                .Field("foo")
                .Resolve("bar"))
            .OnAfterRegisterDependencies<ObjectTypeDefinition>(
                (_, def, _) =>
                {
                    if (def is not null && def.Name.EqualsOrdinal("Query"))
                    {
                        found = true;
                    }
                },
                _ => canHandleInvoked = true));

        // assert
        Assert.True(found);
        Assert.True(canHandleInvoked);
    }

    [Fact]
    public async Task OnBeforeCompleteName()
    {
        // arrange
        Snapshot.FullName();
        var found = false;

        // act
        await CreateSchemaAsync(c => c
            .AddQueryType(d => d
                .Name("Query")
                .Field("foo")
                .Resolve("bar"))
            .OnBeforeCompleteName(
                (_, def, _) =>
                {
                    if (def is not null && def.Name.EqualsOrdinal("Query"))
                    {
                        found = true;
                    }
                }));

        // assert
        Assert.True(found);
    }

    [Fact]
    public async Task OnBeforeCompleteName_Generic()
    {
        // arrange
        Snapshot.FullName();
        var found = false;

        // act
        await CreateSchemaAsync(c => c
            .AddQueryType(d => d
                .Name("Query")
                .Field("foo")
                .Resolve("bar"))
            .OnBeforeCompleteName<ObjectTypeDefinition>(
                (_, def, _) =>
                {
                    if (def is not null && def.Name.EqualsOrdinal("Query"))
                    {
                        found = true;
                    }
                }));

        // assert
        Assert.True(found);
    }

    [Fact]
    public async Task OnBeforeCompleteName_CanHandle()
    {
        // arrange
        Snapshot.FullName();
        var found = false;
        var canHandleInvoked = false;

        // act
        await CreateSchemaAsync(c => c
            .AddQueryType(d => d
                .Name("Query")
                .Field("foo")
                .Resolve("bar"))
            .OnBeforeCompleteName(
                (_, def, _) =>
                {
                    if (def is not null && def.Name.EqualsOrdinal("Query"))
                    {
                        found = true;
                    }
                },
                _ => canHandleInvoked = true));

        // assert
        Assert.True(found);
        Assert.True(canHandleInvoked);
    }

    [Fact]
    public async Task OnBeforeCompleteName_Generic_CanHandle()
    {
        // arrange
        Snapshot.FullName();
        var found = false;
        var canHandleInvoked = false;

        // act
        await CreateSchemaAsync(c => c
            .AddQueryType(d => d
                .Name("Query")
                .Field("foo")
                .Resolve("bar"))
            .OnBeforeCompleteName<ObjectTypeDefinition>(
                (_, def, _) =>
                {
                    if (def is not null && def.Name.EqualsOrdinal("Query"))
                    {
                        found = true;
                    }
                },
                _ => canHandleInvoked = true));

        // assert
        Assert.True(found);
        Assert.True(canHandleInvoked);
    }

    [Fact]
    public async Task OnAfterCompleteName()
    {
        // arrange
        Snapshot.FullName();
        var found = false;

        // act
        await CreateSchemaAsync(c => c
            .AddQueryType(d => d
                .Name("Query")
                .Field("foo")
                .Resolve("bar"))
            .OnAfterCompleteName(
                (_, def, _) =>
                {
                    if (def is not null && def.Name.EqualsOrdinal("Query"))
                    {
                        found = true;
                    }
                }));

        // assert
        Assert.True(found);
    }

    [Fact]
    public async Task OnAfterCompleteName_Generic()
    {
        // arrange
        Snapshot.FullName();
        var found = false;

        // act
        await CreateSchemaAsync(c => c
            .AddQueryType(d => d
                .Name("Query")
                .Field("foo")
                .Resolve("bar"))
            .OnAfterCompleteName<ObjectTypeDefinition>(
                (_, def, _) =>
                {
                    if (def is not null && def.Name.EqualsOrdinal("Query"))
                    {
                        found = true;
                    }
                }));

        // assert
        Assert.True(found);
    }

    [Fact]
    public async Task OnAfterCompleteName_CanHandle()
    {
        // arrange
        Snapshot.FullName();
        var found = false;
        var canHandleInvoked = false;

        // act
        await CreateSchemaAsync(c => c
            .AddQueryType(d => d
                .Name("Query")
                .Field("foo")
                .Resolve("bar"))
            .OnAfterCompleteName(
                (_, def, _) =>
                {
                    if (def is not null && def.Name.EqualsOrdinal("Query"))
                    {
                        found = true;
                    }
                },
                _ => canHandleInvoked = true));

        // assert
        Assert.True(found);
        Assert.True(canHandleInvoked);
    }

    [Fact]
    public async Task OnAfterCompleteName_Generic_CanHandle()
    {
        // arrange
        Snapshot.FullName();
        var found = false;
        var canHandleInvoked = false;

        // act
        await CreateSchemaAsync(c => c
            .AddQueryType(d => d
                .Name("Query")
                .Field("foo")
                .Resolve("bar"))
            .OnAfterCompleteName<ObjectTypeDefinition>(
                (_, def, _) =>
                {
                    if (def is not null && def.Name.EqualsOrdinal("Query"))
                    {
                        found = true;
                    }
                },
                _ => canHandleInvoked = true));

        // assert
        Assert.True(found);
        Assert.True(canHandleInvoked);
    }

    [Fact]
    public async Task OnBeforeCompleteType()
    {
        // arrange
        Snapshot.FullName();
        var found = false;

        // act
        await CreateSchemaAsync(c => c
            .AddQueryType(d => d
                .Name("Query")
                .Field("foo")
                .Resolve("bar"))
            .OnBeforeCompleteType(
                (_, def, _) =>
                {
                    if (def is not null && def.Name.EqualsOrdinal("Query"))
                    {
                        found = true;
                    }
                }));

        // assert
        Assert.True(found);
    }

    [Fact]
    public async Task OnBeforeCompleteType_Generic()
    {
        // arrange
        Snapshot.FullName();
        var found = false;

        // act
        await CreateSchemaAsync(c => c
            .AddQueryType(d => d
                .Name("Query")
                .Field("foo")
                .Resolve("bar"))
            .OnBeforeCompleteType<ObjectTypeDefinition>(
                (_, def, _) =>
                {
                    if (def is not null && def.Name.EqualsOrdinal("Query"))
                    {
                        found = true;
                    }
                }));

        // assert
        Assert.True(found);
    }

    [Fact]
    public async Task OnBeforeCompleteType_CanHandle()
    {
        // arrange
        Snapshot.FullName();
        var found = false;
        var canHandleInvoked = false;

        // act
        await CreateSchemaAsync(c => c
            .AddQueryType(d => d
                .Name("Query")
                .Field("foo")
                .Resolve("bar"))
            .OnBeforeCompleteType(
                (_, def, _) =>
                {
                    if (def is not null && def.Name.EqualsOrdinal("Query"))
                    {
                        found = true;
                    }
                },
                _ => canHandleInvoked = true));

        // assert
        Assert.True(found);
        Assert.True(canHandleInvoked);
    }

    [Fact]
    public async Task OnBeforeCompleteType_Generic_CanHandle()
    {
        // arrange
        Snapshot.FullName();
        var found = false;
        var canHandleInvoked = false;

        // act
        await CreateSchemaAsync(c => c
            .AddQueryType(d => d
                .Name("Query")
                .Field("foo")
                .Resolve("bar"))
            .OnBeforeCompleteType<ObjectTypeDefinition>(
                (_, def, _) =>
                {
                    if (def is not null && def.Name.EqualsOrdinal("Query"))
                    {
                        found = true;
                    }
                },
                _ => canHandleInvoked = true));

        // assert
        Assert.True(found);
        Assert.True(canHandleInvoked);
    }

    [Fact]
    public async Task OnAfterCompleteType()
    {
        // arrange
        Snapshot.FullName();
        var found = false;

        // act
        await CreateSchemaAsync(c => c
            .AddQueryType(d => d
                .Name("Query")
                .Field("foo")
                .Resolve("bar"))
            .OnAfterCompleteType(
                (_, def, _) =>
                {
                    if (def is not null && def.Name.EqualsOrdinal("Query"))
                    {
                        found = true;
                    }
                }));

        // assert
        Assert.True(found);
    }

    [Fact]
    public async Task OnAfterCompleteType_Generic()
    {
        // arrange
        Snapshot.FullName();
        var found = false;

        // act
        await CreateSchemaAsync(c => c
            .AddQueryType(d => d
                .Name("Query")
                .Field("foo")
                .Resolve("bar"))
            .OnAfterCompleteType<ObjectTypeDefinition>(
                (_, def, _) =>
                {
                    if (def is not null && def.Name.EqualsOrdinal("Query"))
                    {
                        found = true;
                    }
                }));

        // assert
        Assert.True(found);
    }

    [Fact]
    public async Task OnAfterCompleteType_CanHandle()
    {
        // arrange
        Snapshot.FullName();
        var found = false;
        var canHandleInvoked = false;

        // act
        await CreateSchemaAsync(c => c
            .AddQueryType(d => d
                .Name("Query")
                .Field("foo")
                .Resolve("bar"))
            .OnAfterCompleteType(
                (_, def, _) =>
                {
                    if (def is not null && def.Name.EqualsOrdinal("Query"))
                    {
                        found = true;
                    }
                },
                _ => canHandleInvoked = true));

        // assert
        Assert.True(found);
        Assert.True(canHandleInvoked);
    }

    [Fact]
    public async Task OnAfterCompleteType_Generic_CanHandle()
    {
        // arrange
        Snapshot.FullName();
        var found = false;
        var canHandleInvoked = false;

        // act
        await CreateSchemaAsync(c => c
            .AddQueryType(d => d
                .Name("Query")
                .Field("foo")
                .Resolve("bar"))
            .OnBeforeCompleteType<ObjectTypeDefinition>(
                (_, def, _) =>
                {
                    if (def is not null && def.Name.EqualsOrdinal("Query"))
                    {
                        found = true;
                    }
                },
                _ => canHandleInvoked = true));

        // assert
        Assert.True(found);
        Assert.True(canHandleInvoked);
    }

    [Fact]
    public async Task OnBeforeSchemaCreate()
    {
        // arrange
        Snapshot.FullName();
        var invoked = false;

        // act
        await CreateSchemaAsync(c => c
            .AddQueryType(d => d
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
        await CreateSchemaAsync(c => c
            .AddQueryType(d => d
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
