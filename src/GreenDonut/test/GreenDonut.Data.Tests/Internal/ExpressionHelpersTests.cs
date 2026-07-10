using System.Linq.Expressions;

namespace GreenDonut.Data.Internal;

public static class ExpressionHelpersTests
{
    [Fact]
    public static void Combine_Should_Use_ArgCarrying_Constructor_When_Parameterless_Init_Comes_First()
    {
        // arrange
        Expression<Func<Entity, Entity>> parameterlessInit = x => new Entity { Category = x.Category };
        Expression<Func<Entity, Entity>> withArgsInit = x => new Entity(x.Id, x.Name);
        var source = new Entity { Id = 1, Name = "abc", Category = "xyz" };

        // act
        var combined = ExpressionHelpers.Combine(parameterlessInit, withArgsInit).Compile();
        var result = combined(source);

        // assert
        Assert.Equal(1, result.Id);
        Assert.Equal("abc", result.Name);
        Assert.Equal("xyz", result.Category);
    }

    [Fact]
    public static void Combine_Should_Use_ArgCarrying_Constructor_When_Parameterless_Init_Comes_Second()
    {
        // arrange
        Expression<Func<Entity, Entity>> withArgsInit = x => new Entity(x.Id, x.Name);
        Expression<Func<Entity, Entity>> parameterlessInit = x => new Entity { Category = x.Category };
        var source = new Entity { Id = 1, Name = "abc", Category = "xyz" };

        // act
        var combined = ExpressionHelpers.Combine(withArgsInit, parameterlessInit).Compile();
        var result = combined(source);

        // assert
        Assert.Equal(1, result.Id);
        Assert.Equal("abc", result.Name);
        Assert.Equal("xyz", result.Category);
    }

    [Fact]
    public static void Combine_Should_Adopt_Constructor_With_More_Arguments_When_Both_Sides_Are_Bare_New()
    {
        // arrange
        Expression<Func<CtorOnlyEntity, CtorOnlyEntity>> fewerArgs = x => new CtorOnlyEntity(x.Id);
        Expression<Func<CtorOnlyEntity, CtorOnlyEntity>> moreArgs = x => new CtorOnlyEntity(x.Id, x.Name);
        var source = new CtorOnlyEntity(1, "abc");

        // act
        var combined = ExpressionHelpers.Combine(fewerArgs, moreArgs).Compile();
        var result = combined(source);

        // assert
        Assert.Equal(1, result.Id);
        Assert.Equal("abc", result.Name);
    }

    [Fact]
    public static void Combine_Should_Preserve_Projection_When_Identity_Comes_First()
    {
        // arrange
        Expression<Func<Entity, Entity>> identity = x => x;
        Expression<Func<Entity, Entity>> projection = x => new Entity { Category = x.Category };
        var source = new Entity { Id = 1, Name = "abc", Category = "xyz" };

        // act
        var combined = ExpressionHelpers.Combine(identity, projection).Compile();
        var result = combined(source);

        // assert
        Assert.Equal(0, result.Id);
        Assert.Equal(string.Empty, result.Name);
        Assert.Equal("xyz", result.Category);
    }

    [Fact]
    public static void Combine_Should_Preserve_Projection_When_Identity_Comes_Second()
    {
        // arrange
        Expression<Func<Entity, Entity>> projection = x => new Entity { Category = x.Category };
        Expression<Func<Entity, Entity>> identity = x => x;
        var source = new Entity { Id = 1, Name = "abc", Category = "xyz" };

        // act
        var combined = ExpressionHelpers.Combine(projection, identity).Compile();
        var result = combined(source);

        // assert
        Assert.Equal(0, result.Id);
        Assert.Equal(string.Empty, result.Name);
        Assert.Equal("xyz", result.Category);
    }

    [Fact]
    public static void Combine_Should_Return_Identity_When_Both_Sides_Are_Identity()
    {
        // arrange
        Expression<Func<Entity, Entity>> first = x => x;
        Expression<Func<Entity, Entity>> second = x => x;
        var source = new Entity { Id = 1, Name = "abc", Category = "xyz" };

        // act
        var combined = ExpressionHelpers.Combine(first, second).Compile();
        var result = combined(source);

        // assert
        Assert.Same(source, result);
    }

    public class Entity
    {
        public Entity()
        {
        }

        public Entity(int id, string name)
        {
            Id = id;
            Name = name;
        }

        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Category { get; set; } = string.Empty;
    }

    public class CtorOnlyEntity
    {
        public CtorOnlyEntity(int id)
        {
            Id = id;
        }

        public CtorOnlyEntity(int id, string name)
        {
            Id = id;
            Name = name;
        }

        public int Id { get; }

        public string Name { get; } = string.Empty;
    }
}
