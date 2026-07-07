using System.Linq.Expressions;

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
        Assert.Equal("93e89ad314d381f1d8e566eb9b83bae0", hash);
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
        Assert.Equal("6f51ab2bb55cc321158bd34a61deabb0", hash1);
        Assert.Equal("56243d626d68ebacdb97e4321bf745a4", hash2);
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
        Assert.Equal("6f51ab2bb55cc321158bd34a61deabb0", hash1);
        Assert.Equal("56243d626d68ebacdb97e4321bf745a4", hash2);
        Assert.Equal("5d06d84fa29de84f9901cbfefa2e0ac0", hash3);
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
                        .Select(t => new Entity2 { Name = t.Name }))
            };

        // act
        var hash = hasher.Add(selector).Compute();

        // assert
        Assert.Equal("78155299ff3ce114f4f60f97b02a00b9", hash);
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
        Assert.Equal("76892c282824bfdfe59e135a298c926e", hash);
    }

    [Fact]
    public static void Predicate_With_Captured_Value_Affects_Hash()
    {
        // arrange
        // Each call captures its argument into a fresh closure, producing a
        // `x.Name == <captured>.value` shape - the same member access rooted at a
        // constant that HotChocolate filtering emits via ExpressionParameter<T>.p.
        var predicate1 = BuildNameEquals("abc");
        var predicate2 = BuildNameEquals("xyz");
        var predicate3 = BuildNameEquals("abc");

        // act
        var hash1 = new ExpressionHasher().Add(predicate1).Compute();
        var hash2 = new ExpressionHasher().Add(predicate2).Compute();
        var hash3 = new ExpressionHasher().Add(predicate3).Compute();

        // assert
        // Different captured values must not collide (regression: they used to,
        // so two aliased fields filtering by eq: A vs eq: B shared one branch).
        Assert.NotEqual(hash1, hash2);
        // Equal captured values must remain stable.
        Assert.Equal(hash1, hash3);
    }

    [Fact]
    public static void Predicate_With_Captured_List_Affects_Hash()
    {
        // arrange
        var predicate1 = BuildNameIn(["a", "b"]);
        var predicate2 = BuildNameIn(["a", "c"]);
        var predicate3 = BuildNameIn(["a", "b"]);

        // act
        var hash1 = new ExpressionHasher().Add(predicate1).Compute();
        var hash2 = new ExpressionHasher().Add(predicate2).Compute();
        var hash3 = new ExpressionHasher().Add(predicate3).Compute();

        // assert
        // `in` filters with different lists must not collide either.
        Assert.NotEqual(hash1, hash2);
        // Equal list contents must stay stable across closure instances.
        Assert.Equal(hash1, hash3);
    }

    [Fact]
    public static void Predicate_With_Captured_List_Element_Boundaries_Affect_Hash()
    {
        // arrange
        // A separator embedded in an element must not make ["a,b", "c"] hash the
        // same as ["a", "b", "c"].
        var predicate1 = BuildNameIn(["a,b", "c"]);
        var predicate2 = BuildNameIn(["a", "b", "c"]);

        // act
        var hash1 = new ExpressionHasher().Add(predicate1).Compute();
        var hash2 = new ExpressionHasher().Add(predicate2).Compute();

        // assert
        Assert.NotEqual(hash1, hash2);
    }

    private static Expression<Func<Entity1, bool>> BuildNameEquals(string value)
        => x => x.Name == value;

    private static Expression<Func<Entity1, bool>> BuildNameIn(string[] values)
        => x => values.Contains(x.Name);

    [Fact]
    public static void BufferResize_Add_Char()
    {
        // arrange
        var hasher = new ExpressionHasher();
        var initialBufferSize = hasher.InitialBufferSize;

        // act
        for (var i = 0; i < initialBufferSize / 2; i++)
        {
            hasher.Add('a');
        }

        Assert.Equal(initialBufferSize, hasher.BufferSize);
        hasher.Add('b');

        // assert
        Assert.Equal(initialBufferSize * 2, hasher.BufferSize);
    }

    [Fact]
    public static void BufferResize_Add_ReadOnlyCharSpan()
    {
        // arrange
        var hasher = new ExpressionHasher();
        var initialBufferSize = hasher.InitialBufferSize;

        // act
        hasher.Add(new string('a', initialBufferSize + 1));

        // assert
        Assert.Equal(initialBufferSize * 2, hasher.BufferSize);
    }

    [Fact]
    public static void BufferResize_Add_ReadOnlyByteSpan()
    {
        // arrange
        var hasher = new ExpressionHasher();
        var initialBufferSize = hasher.InitialBufferSize;

        // act
        hasher.Add(Enumerable.Range(0, initialBufferSize + 1).Select(_ => (byte)1).ToArray());

        // assert
        Assert.Equal(initialBufferSize * 2, hasher.BufferSize);
    }

    [Fact]
    public static void BufferResize_Add_Expression()
    {
        // arrange
        var hasher = new ExpressionHasher();
        var initialBufferSize = hasher.InitialBufferSize;
        var expression = Expression.Lambda<Func<Entity1>>(Expression.Constant(new Entity1())); // translated to 8 bytes

        // act
        var length = 0;
        while (length < initialBufferSize + 1)
        {
            hasher.Add(expression);
            length += 8;
        }

        // assert
        Assert.Equal(initialBufferSize * 2, hasher.BufferSize);
    }

    [Fact]
    public static void BufferResize_Add_QueryContext()
    {
        // arrange
        var hasher = new ExpressionHasher();
        var initialBufferSize = hasher.InitialBufferSize;
        var queryContext = new QueryContext<Entity1>(x => x); // translated to 32 bytes

        // act
        var length = 0;
        while (length < initialBufferSize + 1)
        {
            hasher.Add(queryContext);
            length += 32;
        }

        // assert
        Assert.Equal(initialBufferSize * 2, hasher.BufferSize);
    }

    [Fact]
    public static void BufferResize_Add_SortDefinition()
    {
        // arrange
        var hasher = new ExpressionHasher();
        var initialBufferSize = hasher.InitialBufferSize;
        var sortDefinition = new SortDefinition<Entity1>().AddAscending(x => x.Name); // translated to 102 bytes

        // act
        var length = 0;
        while (length < initialBufferSize + 1)
        {
            hasher.Add(sortDefinition);
            length += 102;
        }

        // assert
        Assert.Equal(initialBufferSize * 2, hasher.BufferSize);
    }

    public class Entity1
    {
        public string Name { get; set; } = null!;

        public string? Description { get; set; }

        public IEntity Entity { get; set; } = null!;

        public List<IEntity> Entities { get; set; } = null!;
    }

    public interface IEntity;

    public class Entity2 : IEntity
    {
        public string Name { get; set; } = null!;
    }

    public class Entity3 : IEntity
    {
        public string Name { get; set; } = null!;
    }
}
