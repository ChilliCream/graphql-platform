using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Types.Descriptors;

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

    public class Foo : IConvention
    {
        public string? Scope { get; set; }
    }
}
