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
        Assert.Equal("c813ce43dee13611a2ea72f044810ec0", hash);
    }

    [Fact]
    public static void Predicate_With_Different_Constants_Hashes_Differently()
    {
        // arrange
        Expression<Func<Entity1, bool>> p1 = x => x.Name == "abc";
        Expression<Func<Entity1, bool>> p2 = x => x.Name == "xyz";

        // act
        var hash1 = new ExpressionHasher().Add(p1).Compute();
        var hash2 = new ExpressionHasher().Add(p2).Compute();

        // assert
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public static void Predicate_With_Captured_Variable_Hashes_By_Value()
    {
        // arrange
        // Captured locals become ConstantExpression instances wrapping a compiler-generated
        // closure; the hash must reflect the captured value, not just the closure type.

        // act
        var hash1 = HashWithCapturedName("abc");
        var hash2 = HashWithCapturedName("xyz");

        // assert
        Assert.NotEqual(hash1, hash2);
    }

    private static string HashWithCapturedName(string name)
    {
        Expression<Func<Entity1, bool>> predicate = x => x.Name == name;
        return new ExpressionHasher().Add(predicate).Compute();
    }

    [Fact]
    public static void Predicate_With_NonPublicField_Wrapper_Hashes_By_Value()
    {
        // arrange
        var wrapper1 = new NonPublicFieldWrapper("abc");
        var wrapper2 = new NonPublicFieldWrapper("xyz");
        Expression<Func<Entity1, bool>> p1 = Expression.Lambda<Func<Entity1, bool>>(
            Expression.Equal(
                Expression.Property(Expression.Parameter(typeof(Entity1), "x"), nameof(Entity1.Name)),
                Expression.Field(Expression.Constant(wrapper1), "_value")),
            Expression.Parameter(typeof(Entity1), "x"));
        Expression<Func<Entity1, bool>> p2 = Expression.Lambda<Func<Entity1, bool>>(
            Expression.Equal(
                Expression.Property(Expression.Parameter(typeof(Entity1), "x"), nameof(Entity1.Name)),
                Expression.Field(Expression.Constant(wrapper2), "_value")),
            Expression.Parameter(typeof(Entity1), "x"));

        // act
        var hash1 = new ExpressionHasher().Add(p1).Compute();
        var hash2 = new ExpressionHasher().Add(p2).Compute();

        // assert
        Assert.NotEqual(hash1, hash2);
    }

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
        var expression = Expression.Lambda<Func<int>>(Expression.Constant(0));

        // act
        while (hasher.BufferSize == initialBufferSize)
        {
            hasher.Add(expression);
        }

        // assert
        Assert.True(hasher.BufferSize > initialBufferSize);
    }

    [Fact]
    public static void BufferResize_Add_QueryContext()
    {
        // arrange
        var hasher = new ExpressionHasher();
        var initialBufferSize = hasher.InitialBufferSize;
        var queryContext = new QueryContext<Entity1>(x => x);

        // act
        while (hasher.BufferSize == initialBufferSize)
        {
            hasher.Add(queryContext);
        }

        // assert
        Assert.True(hasher.BufferSize > initialBufferSize);
    }

    [Fact]
    public static void BufferResize_Add_SortDefinition()
    {
        // arrange
        var hasher = new ExpressionHasher();
        var initialBufferSize = hasher.InitialBufferSize;
        var sortDefinition = new SortDefinition<Entity1>().AddAscending(x => x.Name);

        // act
        while (hasher.BufferSize == initialBufferSize)
        {
            hasher.Add(sortDefinition);
        }

        // assert
        Assert.True(hasher.BufferSize > initialBufferSize);
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

#pragma warning disable RCS1169
    public class NonPublicFieldWrapper
    {
        private readonly string _value;

        public NonPublicFieldWrapper(string value)
        {
            _value = value;
        }
    }
#pragma warning restore RCS1169
}
