using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using Xunit;

#nullable enable

namespace HotChocolate.Execution.DependencyInjection;

public class SchemaRequestExecutorBuilderExtensionsConventionsTests
{
    [Fact]
    public void AddConventionWithFactory_BuilderNull()
    {
        void Verify() => default(ServiceCollection)!
            .AddGraphQL()
            .AddConvention(typeof(Foo), _ => new Foo());

        Assert.Throws<ArgumentNullException>(Verify);
    }

    [Fact]
    public void AddConventionWithFactory_TypeNull()
    {
        void Verify() => new ServiceCollection()
            .AddGraphQL()
            .AddConvention(default!, _ => new Foo());

        Assert.Throws<ArgumentNullException>(Verify);
    }

    [Fact]
    public void AddConventionWithFactory_FactoryNull()
    {
        void Verify() => new ServiceCollection()
            .AddGraphQL()
            .AddConvention(typeof(Foo), default(CreateConvention)!);

        Assert.Throws<ArgumentNullException>(Verify);
    }

    [Fact]
    public async Task AddConventionWithFactory()
    {
        var conventionCreated = false;

        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(d => d.Name("Query").Field("foo").Resolve("bar"))
            .AddConvention(typeof(Foo), _ =>
            {
                conventionCreated = true;
                return new Foo();
            })
            .OnBeforeCompleteType((c, _, _) =>
            {
                c.DescriptorContext.GetConventionOrDefault<Foo>(
                    () => throw new NotSupportedException());
            })
            .BuildSchemaAsync();

        Assert.True(conventionCreated);
    }

    [Fact]
    public async Task AddConventionWithFactoryAndScope()
    {
        var conventionCreated = false;

        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(d => d.Name("Query").Field("foo").Resolve("bar"))
            .AddConvention(typeof(Foo), _ =>
            {
                conventionCreated = true;
                return new Foo();
            }, "bar")
            .OnBeforeCompleteType((c, _, _) =>
            {
                c.DescriptorContext.GetConventionOrDefault<Foo>(
                    () => throw new NotSupportedException(),
                    "bar");
            })
            .BuildSchemaAsync();

        Assert.True(conventionCreated);
    }

    [Fact]
    public void AddConventionWithType_BuilderNull()
    {
        void Verify() => default(ServiceCollection)!
            .AddGraphQL()
            .AddConvention<IConvention>(typeof(Foo));

        Assert.Throws<ArgumentNullException>(Verify);
    }

    [Fact]
    public void AddConventionWithType_TypeNull()
    {
        void Verify() => new ServiceCollection()
            .AddGraphQL()
            .AddConvention<IConvention>(default(Type)!);

        Assert.Throws<ArgumentNullException>(Verify);
    }

    [Fact]
    public async Task AddConventionWithType()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(d => d.Name("Query").Field("foo").Resolve("bar"))
            .AddConvention<Foo>(typeof(Foo))
            .OnBeforeCompleteType((c, _, _) =>
            {
                c.DescriptorContext.GetConventionOrDefault<Foo>(
                    () => throw new NotSupportedException());
            })
            .BuildSchemaAsync();
    }

    [Fact]
    public void TryAddConventionWithFactory_BuilderNull()
    {
        void Verify() => default(ServiceCollection)!
            .AddGraphQL()
            .TryAddConvention(typeof(Foo), _ => new Foo());

        Assert.Throws<ArgumentNullException>(Verify);
    }

    [Fact]
    public void TryAddConventionWithFactory_TypeNull()
    {
        void Verify() => new ServiceCollection()
            .AddGraphQL()
            .TryAddConvention(default!, _ => new Foo());

        Assert.Throws<ArgumentNullException>(Verify);
    }

    [Fact]
    public void TryAddConventionWithFactory_FactoryNull()
    {
        void Verify() => new ServiceCollection()
            .AddGraphQL()
            .TryAddConvention(typeof(Foo), default(CreateConvention)!);

        Assert.Throws<ArgumentNullException>(Verify);
    }

    [Fact]
    public async Task TryAddConventionWithFactory()
    {
        var conventionCreated = false;

        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(d => d.Name("Query").Field("foo").Resolve("bar"))
            .TryAddConvention(typeof(Foo), _ =>
            {
                conventionCreated = true;
                return new Foo();
            })
            .OnBeforeCompleteType((c, _, _) =>
            {
                c.DescriptorContext.GetConventionOrDefault<Foo>(
                    () => throw new NotSupportedException());
            })
            .BuildSchemaAsync();

        Assert.True(conventionCreated);
    }

    [Fact]
    public async Task TryAddConventionWithFactoryAndScope()
    {
        var conventionCreated = false;

        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(d => d.Name("Query").Field("foo").Resolve("bar"))
            .TryAddConvention(typeof(Foo), _ =>
            {
                conventionCreated = true;
                return new Foo();
            }, "bar")
            .OnBeforeCompleteType((c, _, _) =>
            {
                c.DescriptorContext.GetConventionOrDefault<Foo>(
                    () => throw new NotSupportedException(),
                    "bar");
            })
            .BuildSchemaAsync();

        Assert.True(conventionCreated);
    }

    public class Foo : IConvention
    {
        public string? Scope { get; set; }
    }
}