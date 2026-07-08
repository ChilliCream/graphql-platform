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
        Assert.Equal("9d85f008b51c17f727b1ccf836d28d02", hash);
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
        Assert.Equal("a6291badbb36d78585568bddc5fc334f", hash);
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
        var expression = Expression.Lambda<Func<Entity1>>(Expression.Constant(new Entity1()));

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

    [Fact]
    public static void Hoisted_Eq_Different_Value_Should_Produce_Different_Hash()
    {
        // arrange
        var hasher = new ExpressionHasher();
        var param = Expression.Parameter(typeof(Entity1), "x");
        var expression1 = Expression.Lambda<Func<Entity1, bool>>(
            Expression.Equal(
                Expression.Property(param, nameof(Entity1.Name)),
                Expression.Property(
                    Expression.Constant(new ExpressionParameterMirror<string>("abc")),
                    "p")),
            param);
        var expression2 = Expression.Lambda<Func<Entity1, bool>>(
            Expression.Equal(
                Expression.Property(param, nameof(Entity1.Name)),
                Expression.Property(
                    Expression.Constant(new ExpressionParameterMirror<string>("xyz")),
                    "p")),
            param);

        // act
        var hash1 = hasher.Add(expression1).Compute();
        var hash2 = hasher.Add(expression2).Compute();

        // assert
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public static void Hoisted_Eq_Same_Value_Should_Produce_Same_Hash()
    {
        // arrange
        var hasher = new ExpressionHasher();
        var param = Expression.Parameter(typeof(Entity1), "x");
        var expression1 = Expression.Lambda<Func<Entity1, bool>>(
            Expression.Equal(
                Expression.Property(param, nameof(Entity1.Name)),
                Expression.Property(
                    Expression.Constant(new ExpressionParameterMirror<string>("abc")),
                    "p")),
            param);
        var expression2 = Expression.Lambda<Func<Entity1, bool>>(
            Expression.Equal(
                Expression.Property(param, nameof(Entity1.Name)),
                Expression.Property(
                    Expression.Constant(new ExpressionParameterMirror<string>("abc")),
                    "p")),
            param);

        // act
        var hash1 = hasher.Add(expression1).Compute();
        var hash2 = hasher.Add(expression2).Compute();

        // assert
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public static void Closure_Captured_Different_Value_Should_Produce_Different_Hash()
    {
        // arrange
        var hasher = new ExpressionHasher();
        var expression1 = Build("abc");
        var expression2 = Build("xyz");

        // act
        var hash1 = hasher.Add(expression1).Compute();
        var hash2 = hasher.Add(expression2).Compute();

        // assert
        Assert.NotEqual(hash1, hash2);

        static Expression<Func<Entity1, bool>> Build(string v) => x => x.Name == v;
    }

    [Fact]
    public static void Bare_Constant_Different_Value_Should_Produce_Different_Hash()
    {
        // arrange
        var hasher = new ExpressionHasher();
        var param = Expression.Parameter(typeof(Entity1), "x");
        var expression1 = Expression.Lambda<Func<Entity1, bool>>(
            Expression.Equal(
                Expression.Property(param, nameof(Entity1.Name)),
                Expression.Constant("abc")),
            param);
        var expression2 = Expression.Lambda<Func<Entity1, bool>>(
            Expression.Equal(
                Expression.Property(param, nameof(Entity1.Name)),
                Expression.Constant("xyz")),
            param);

        // act
        var hash1 = hasher.Add(expression1).Compute();
        var hash2 = hasher.Add(expression2).Compute();

        // assert
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public static void Hoisted_In_List_Different_Elements_Should_Produce_Different_Hash()
    {
        // arrange
        var hasher = new ExpressionHasher();
        var param = Expression.Parameter(typeof(Entity1), "x");
        var containsMethod = typeof(Enumerable)
            .GetMethods()
            .Single(m => m.Name == "Contains" && m.GetParameters().Length == 2)
            .MakeGenericMethod(typeof(string));
        var expression1 = Expression.Lambda<Func<Entity1, bool>>(
            Expression.Call(
                containsMethod,
                Expression.Property(
                    Expression.Constant(
                        new ExpressionParameterMirror<IEnumerable<string>>(new[] { "a", "b" })),
                    "p"),
                Expression.Property(param, nameof(Entity1.Name))),
            param);
        var expression2 = Expression.Lambda<Func<Entity1, bool>>(
            Expression.Call(
                containsMethod,
                Expression.Property(
                    Expression.Constant(
                        new ExpressionParameterMirror<IEnumerable<string>>(new[] { "a", "c" })),
                    "p"),
                Expression.Property(param, nameof(Entity1.Name))),
            param);

        // act
        var hash1 = hasher.Add(expression1).Compute();
        var hash2 = hasher.Add(expression2).Compute();

        // assert
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public static void Hoisted_Bool_Different_Value_Should_Produce_Different_Hash()
    {
        // arrange
        var hasher = new ExpressionHasher();
        var param = Expression.Parameter(typeof(Entity1), "x");
        var expression1 = Expression.Lambda<Func<Entity1, bool>>(
            Expression.Equal(
                Expression.Property(param, nameof(Entity1.IsActive)),
                Expression.Property(
                    Expression.Constant(new ExpressionParameterMirror<bool>(true)),
                    "p")),
            param);
        var expression2 = Expression.Lambda<Func<Entity1, bool>>(
            Expression.Equal(
                Expression.Property(param, nameof(Entity1.IsActive)),
                Expression.Property(
                    Expression.Constant(new ExpressionParameterMirror<bool>(false)),
                    "p")),
            param);

        // act
        var hash1 = hasher.Add(expression1).Compute();
        var hash2 = hasher.Add(expression2).Compute();

        // assert
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public static void Throwing_Property_Getter_On_Reference_Holder_Should_Not_Be_Invoked()
    {
        // arrange
        var holder = new ThrowingPropertyHolder();
        var expression = Expression.Property(
            Expression.Constant(holder),
            nameof(ThrowingPropertyHolder.Value));

        // act
        var exception = Record.Exception(() => new ExpressionHasher().Add(expression).Compute());

        // assert
        Assert.Null(exception);
        Assert.False(holder.WasInvoked);
    }

    [Fact]
    public static void Lazy_Enumerable_Should_Not_Be_Enumerated()
    {
        // arrange
        var enumerable = new ThrowingEnumerable();
        var expression = Expression.Constant(enumerable);

        // act
        var exception = Record.Exception(() => new ExpressionHasher().Add(expression).Compute());

        // assert
        Assert.Null(exception);
        Assert.False(enumerable.Enumerated);
    }

    [Fact]
    public static void Queryable_Should_Not_Be_Enumerated()
    {
        // arrange
        var queryable = new ThrowingQueryable();
        var expression = Expression.Constant(queryable);

        // act
        var exception = Record.Exception(() => new ExpressionHasher().Add(expression).Compute());

        // assert
        Assert.Null(exception);
        Assert.False(queryable.Enumerated);
    }

    [Fact]
    public static void Self_Referential_Collection_Should_Terminate_And_Be_Deterministic()
    {
        // arrange
        var self = new object[2];
        self[0] = self;
        self[1] = "x";
        var expression = Expression.Constant(self);

        // act
        string? hash1 = null;
        string? hash2 = null;
        var exception1 = Record.Exception(() => hash1 = new ExpressionHasher().Add(expression).Compute());
        var exception2 = Record.Exception(() => hash2 = new ExpressionHasher().Add(expression).Compute());

        // assert
        Assert.Null(exception1);
        Assert.Null(exception2);
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public static void Captured_DateTime_Sub_Second_Difference_Should_Produce_Different_Hash()
    {
        // arrange
        var value1 = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddTicks(1_230_000);
        var value2 = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddTicks(4_560_000);

        // act
        var hash1 = HashConstant(value1);
        var hash2 = HashConstant(value2);

        // assert
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public static void Captured_List_Element_Boundaries_Should_Produce_Different_Hash()
    {
        // arrange
        var value1 = new[] { "a,b" };
        var value2 = new[] { "a", "b" };

        // act
        var hash1 = HashConstant(value1);
        var hash2 = HashConstant(value2);

        // assert
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public static void Same_List_Values_Different_Instances_Should_Produce_Same_Hash()
    {
        // arrange
        var value1 = new[] { "a", "b" };
        var value2 = new[] { "a", "b" };

        // act
        var hash1 = HashConstant(value1);
        var hash2 = HashConstant(value2);

        // assert
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public static void Constant_Int_And_String_As_Object_Should_Produce_Different_Hash()
    {
        // arrange
        var expression1 = Expression.Constant(1, typeof(object));
        var expression2 = Expression.Constant("1", typeof(object));

        // act
        var hash1 = new ExpressionHasher().Add(expression1).Compute();
        var hash2 = new ExpressionHasher().Add(expression2).Compute();

        // assert
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public static void Long_String_Should_Hash_Deterministically_And_Distinguish_Prefix()
    {
        // arrange
        var longValue = new string('a', 5000);
        var prefixed1 = "A" + new string('a', 5000);
        var prefixed2 = "B" + new string('a', 5000);

        // act
        var hash1 = HashConstant(longValue);
        var hash2 = HashConstant(longValue);
        var prefixedHash1 = HashConstant(prefixed1);
        var prefixedHash2 = HashConstant(prefixed2);

        // assert
        Assert.Equal(hash1, hash2);
        Assert.NotEqual(prefixedHash1, prefixedHash2);
    }

    [Fact]
    public static void Bare_Bool_Constants_Should_Produce_Different_Hash()
    {
        // arrange
        var expression1 = Expression.Constant(true);
        var expression2 = Expression.Constant(false);

        // act
        var hash1 = new ExpressionHasher().Add(expression1).Compute();
        var hash2 = new ExpressionHasher().Add(expression2).Compute();

        // assert
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public static void Captured_Record_Struct_Different_Value_Should_Produce_Different_Hash()
    {
        // arrange
        var value1 = new StructKey(1, "a");
        var value2 = new StructKey(2, "a");

        // act
        var hash1 = HashConstant(value1);
        var hash2 = HashConstant(value2);
        var hash3 = HashConstant(new StructKey(1, "a"));

        // assert
        Assert.NotEqual(hash1, hash2);
        Assert.Equal(hash1, hash3);
    }

    [Fact]
    public static void Captured_Tuple_Member_Chain_Should_Produce_Different_Hash()
    {
        // arrange
        var expression1 = BuildTuplePredicate(("a", "b"));
        var expression2 = BuildTuplePredicate(("c", "d"));

        // act
        var hash1 = new ExpressionHasher().Add(expression1).Compute();
        var hash2 = new ExpressionHasher().Add(expression2).Compute();

        // assert
        Assert.NotEqual(hash1, hash2);

        static Expression<Func<Entity1, bool>> BuildTuplePredicate((string A, string B) pair)
            => x => x.Name == pair.A;
    }

    [Fact]
    public static void Formattable_Struct_List_Boundaries_Should_Produce_Different_Hash()
    {
        // arrange
        // A payload that reproduces the per-element header could previously shift element
        // boundaries so that two different lists encoded to identical bytes.
        var header = "N" + (char)103 + ":" + typeof(AmbiguousFormattable).FullName + "|";
        var value1 = new[] { new AmbiguousFormattable("1" + header + "2"), new AmbiguousFormattable("3") };
        var value2 = new[] { new AmbiguousFormattable("1"), new AmbiguousFormattable("2" + header + "3") };

        // act
        var hash1 = HashConstant(value1);
        var hash2 = HashConstant(value2);

        // assert
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public static void Type_Check_Predicate_Different_Type_Should_Produce_Different_Hash()
    {
        // arrange
        Expression<Func<Entity1, bool>> expression1 = x => x.Entity is Entity2;
        Expression<Func<Entity1, bool>> expression2 = x => x.Entity is Entity3;

        // act
        var hash1 = new ExpressionHasher().Add(expression1).Compute();
        var hash2 = new ExpressionHasher().Add(expression2).Compute();

        // assert
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public static void Generic_Method_Argument_Should_Produce_Different_Hash()
    {
        // arrange
        Expression<Func<Entity1, bool>> expression1 = x => x.Entities.OfType<Entity2>().Any();
        Expression<Func<Entity1, bool>> expression2 = x => x.Entities.OfType<Entity3>().Any();

        // act
        var hash1 = new ExpressionHasher().Add(expression1).Compute();
        var hash2 = new ExpressionHasher().Add(expression2).Compute();

        // assert
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public static void Large_Captured_List_Should_Be_Bounded_And_Deterministic()
    {
        // arrange
        var value = new int[100_000];
        var hasher = new ExpressionHasher();

        // act
        hasher.Add(Expression.Constant(value));
        var bufferSize = hasher.BufferSize;
        var hash1 = hasher.Compute();
        var hash2 = HashConstant(value);

        // assert
        Assert.True(bufferSize < 1_048_576, $"buffer grew to {bufferSize} bytes");
        Assert.Equal(hash1, hash2);
    }

    private static string HashConstant(object value)
        => new ExpressionHasher().Add(Expression.Constant(value)).Compute();

    private readonly record struct ExpressionParameterMirror<T>(T p);

    private readonly record struct StructKey(int Id, string Name);

    private readonly struct AmbiguousFormattable(string text) : IFormattable
    {
        public string ToString(string? format, IFormatProvider? formatProvider) => text;
    }

    private sealed class ThrowingPropertyHolder
    {
        public bool WasInvoked { get; private set; }

        public string Value
        {
            get
            {
                WasInvoked = true;
                throw new InvalidOperationException();
            }
        }
    }

    private sealed class ThrowingEnumerable : IEnumerable<string>
    {
        public bool Enumerated { get; private set; }

        public IEnumerator<string> GetEnumerator()
        {
            Enumerated = true;
            throw new InvalidOperationException();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            => GetEnumerator();
    }

    private sealed class ThrowingQueryable : IQueryable<string>
    {
        public bool Enumerated { get; private set; }

        public Type ElementType => typeof(string);

        public Expression Expression => System.Linq.Expressions.Expression.Constant(this);

        public IQueryProvider Provider => throw new InvalidOperationException();

        public IEnumerator<string> GetEnumerator()
        {
            Enumerated = true;
            throw new InvalidOperationException();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            => GetEnumerator();
    }

    public class Entity1
    {
        public string Name { get; set; } = null!;

        public string? Description { get; set; }

        public bool IsActive { get; set; }

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
