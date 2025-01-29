using System.Linq.Expressions;
using Xunit;

namespace GreenDonut.Data.Internal;

public static class ExpressionHasherTests
{
    [Fact]
    public static void Simple_Field_Selector()
    {
        // arrange
        var hasher = new ExpressionHasher();
        Expression<Func<Entity1, string>> selector = x => x.Name;

        // act
        var hash = hasher.Add(selector).Compute();

        // assert
        Assert.Equal("3bbe9acfc37825818d75547df2802080", hash);
    }

    [Fact]
    public static void Selector_With_Interface()
    {
        // arrange
        var hasher = new ExpressionHasher();
        Expression<Func<Entity1, Entity1>> selector1 =
            x => new Entity1 { Entity = new Entity2 { Name = ((Entity2)x.Entity).Name } };
        Expression<Func<Entity1, Entity1>> selector2 =
            x => new Entity1 { Entity = new Entity3 { Name = ((Entity3)x.Entity).Name } };

        // act
        var hash1 = hasher.Add(selector1).Compute();
        var hash2 = hasher.Add(selector2).Compute();

        // assert
        Assert.Equal("dbf2f60aa7fa00fe4856894cb4ebe8fc", hash1);
        Assert.Equal("3662ca2896134511f1dc130120d5005f", hash2);
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public static void Selector_With_Interface_And_Combine()
    {
        // arrange
        var hasher = new ExpressionHasher();
        Expression<Func<Entity1, Entity1>> selector1 =
            x => new Entity1 { Entity = new Entity2 { Name = ((Entity2)x.Entity).Name } };
        Expression<Func<Entity1, Entity1>> selector2 =
            x => new Entity1 { Entity = new Entity3 { Name = ((Entity3)x.Entity).Name } };

        // act
        var hash1 = hasher.Add(selector1).Compute();
        var hash2 = hasher.Add(selector2).Compute();
        var hash3 = hasher.Add(selector1).Add(selector2).Compute();

        // assert
        Assert.Equal("dbf2f60aa7fa00fe4856894cb4ebe8fc", hash1);
        Assert.Equal("3662ca2896134511f1dc130120d5005f", hash2);
        Assert.Equal("3dc320227bb0fe57f35be799bad69975", hash3);
        Assert.NotEqual(hash1, hash2);
        Assert.NotEqual(hash1, hash3);
    }

    [Fact]
    public static void Selector_With_List()
    {
        // arrange
        var hasher = new ExpressionHasher();
        Expression<Func<Entity1, Entity1>> selector =
            x => new Entity1
            {
                Entities = new List<IEntity>(
                    x.Entities
                        .OfType<Entity2>()
                        .Select(t => new Entity2 { Name = t.Name } ))
            };

        // act
        var hash = hasher.Add(selector).Compute();

        // assert
        Assert.Equal("bcee7ce84e8e24974afa2d4e121c2116", hash);
    }

    [Fact]
    public static void Simple_Predicate()
    {
        // arrange
        var hasher = new ExpressionHasher();
        Expression<Func<Entity1, bool>> selector = x => x.Name == "abc";

        // act
        var hash = hasher.Add(selector).Compute();

        // assert
        Assert.Equal("4ca93a1aed69ddc7269106fd807109b1", hash);
    }

    public class Entity1
    {
        public string Name { get; set; } = default!;

        public string? Description { get; set; }

        public IEntity Entity { get; set; } = default!;

        public List<IEntity> Entities { get; set; } = default!;
    }

    public interface IEntity;

    public class Entity2 : IEntity
    {
        public string Name { get; set; } = default!;
    }

    public class Entity3 : IEntity
    {
        public string Name { get; set; } = default!;
    }
}
