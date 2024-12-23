using System.Text;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;
using static HotChocolate.Types.Interceptors.FlagEnumInterceptorTests.FlagsEnum;

namespace HotChocolate.Types.Interceptors;

#nullable enable

public class FlagEnumInterceptorTests
{
    [Fact]
    public async Task Schema_Should_Generate_Outputs()
    {
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<OutputQuery>()
            .ModifyOptions(x => x.EnableFlagEnums = true)
            .BuildRequestExecutorAsync();
        executor.Schema.Print().MatchSnapshot();
    }

    [Fact]
    public async Task Schema_Should_Generate_Description()
    {
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(
                x
                    => x.Name("Query")
                        .Field("asd")
                        .Argument("input", x => x.Type(typeof(FlagsWithDescription)))
                        .Resolve(FlagsWithDescription.Bar))
            .ModifyOptions(x => x.EnableFlagEnums = true)
            .BuildRequestExecutorAsync();
        executor.Schema.Print().MatchSnapshot();
    }

    [Fact]
    public async Task Schema_Should_Generate_Interface()
    {
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(
                x
                    => x.Name("Query").Field("asd").Resolve("baz").Type<InterfaceType<Interface>>())
            .AddType<Impl>()
            .ModifyOptions(x => x.EnableFlagEnums = true)
            .BuildRequestExecutorAsync();
        executor.Schema.Print().MatchSnapshot();
    }

    [Fact]
    public async Task Schema_Should_Generate_Directive()
    {
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(
                x
                    => x.Name("Query").Field("asd").Resolve("baz"))
            .AddDirectiveType(
                new DirectiveType(
                    x
                        => x.Name("Test")
                            .Location(DirectiveLocation.FragmentSpread)
                            .Argument("a")
                            .Type(typeof(FlagsEnum))))
            .ModifyOptions(x => x.EnableFlagEnums = true)
            .BuildRequestExecutorAsync();
        executor.Schema.Print().MatchSnapshot();
    }

    [Fact]
    public async Task Schema_Should_Generate_Inputs()
    {
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<InputQuery>()
            .ModifyOptions(x => x.EnableFlagEnums = true)
            .BuildRequestExecutorAsync();
        executor.Schema.Print().MatchSnapshot();
    }

    [Fact]
    public async Task Schema_Should_Return_Scalar()
    {
        var executor1 = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(x => x.Name("Query").Field("test").Resolve(Bar))
            .ModifyOptions(x => x.EnableFlagEnums = true)
            .BuildRequestExecutorAsync();
        var result1 = await executor1.ExecuteAsync("{ test {isBar isBaz isFoo }}");

        var executor2 = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(x => x.Name("Query").Field("test").Resolve(Baz | Bar))
            .ModifyOptions(x => x.EnableFlagEnums = true)
            .BuildRequestExecutorAsync();
        var result2 = await executor2.ExecuteAsync("{ test {isBar isBaz isFoo }}");

        var executor3 = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(x => x.Name("Query").Field("test").Resolve(Baz | Bar | FlagsEnum.Foo))
            .ModifyOptions(x => x.EnableFlagEnums = true)
            .BuildRequestExecutorAsync();
        var result3 = await executor3.ExecuteAsync("{ test {isBar isBaz isFoo }}");

        new StringBuilder()
            .AppendLine("Bar:")
            .AppendLine(result1.ToJson())
            .AppendLine("Bar Baz:")
            .AppendLine(result2.ToJson())
            .AppendLine("All")
            .AppendLine(result3.ToJson())
            .ToString()
            .MatchSnapshot();
    }

    [Fact]
    public async Task Schema_Should_Return_Lists()
    {
        var executor1 = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(x => x.Name("Query").Field("test").Resolve(new[] { Bar, }))
            .ModifyOptions(x => x.EnableFlagEnums = true)
            .BuildRequestExecutorAsync();
        var result1 = await executor1.ExecuteAsync("{ test {isBar isBaz isFoo }}");

        var executor2 = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(x => x.Name("Query").Field("test").Resolve(new[] { new[] { Baz | Bar, }, }))
            .ModifyOptions(x => x.EnableFlagEnums = true)
            .BuildRequestExecutorAsync();
        var result2 = await executor2.ExecuteAsync("{ test {isBar isBaz isFoo }}");

        new StringBuilder()
            .AppendLine("List:")
            .AppendLine(result1.ToJson())
            .AppendLine("NestedList:")
            .AppendLine(result2.ToJson())
            .ToString()
            .MatchSnapshot();
    }

    [Fact]
    public async Task Schema_Should_Return_Scalar_Nullable()
    {
        var executor1 = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(x => x.Name("Query").Field("test").Resolve((FlagsEnum?)Bar))
            .ModifyOptions(x => x.EnableFlagEnums = true)
            .BuildRequestExecutorAsync();
        var result1 = await executor1.ExecuteAsync("{ test {isBar isBaz isFoo }}");

        var executor2 = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(x => x.Name("Query").Field("test").Resolve((FlagsEnum?)Baz | Bar))
            .ModifyOptions(x => x.EnableFlagEnums = true)
            .BuildRequestExecutorAsync();
        var result2 = await executor2.ExecuteAsync("{ test {isBar isBaz isFoo }}");

        var executor3 = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(
                x
                    => x.Name("Query").Field("test").Resolve((FlagsEnum?)Baz | Bar | FlagsEnum.Foo))
            .ModifyOptions(x => x.EnableFlagEnums = true)
            .BuildRequestExecutorAsync();
        var result3 = await executor3.ExecuteAsync("{ test {isBar isBaz isFoo }}");

        var executor4 = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(x => x.Name("Query").Field("test").Resolve((FlagsEnum?)null))
            .ModifyOptions(x => x.EnableFlagEnums = true)
            .BuildRequestExecutorAsync();
        var result4 = await executor4.ExecuteAsync("{ test {isBar isBaz isFoo }}");

        new StringBuilder()
            .AppendLine("Bar:")
            .AppendLine(result1.ToJson())
            .AppendLine("Bar Baz:")
            .AppendLine(result2.ToJson())
            .AppendLine("All")
            .AppendLine(result3.ToJson())
            .AppendLine("Null")
            .AppendLine(result4.ToJson())
            .ToString()
            .MatchSnapshot();
    }

    [Fact]
    public async Task Schema_Should_Return_Lists_Nullable()
    {
        var executor1 = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(x => x.Name("Query").Field("test").Resolve(new FlagsEnum?[] { Bar, }))
            .ModifyOptions(x => x.EnableFlagEnums = true)
            .BuildRequestExecutorAsync();
        var result1 = await executor1.ExecuteAsync("{ test {isBar isBaz isFoo }}");

        var executor2 = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(
                x
                    => x.Name("Query").Field("test")
                        .Resolve(new[] { new FlagsEnum?[] { Baz | Bar, }, }))
            .ModifyOptions(x => x.EnableFlagEnums = true)
            .BuildRequestExecutorAsync();
        var result2 = await executor2.ExecuteAsync("{ test {isBar isBaz isFoo }}");

        var executor3 = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(x => x.Name("Query").Field("test").Resolve(new FlagsEnum?[] { null, }))
            .ModifyOptions(x => x.EnableFlagEnums = true)
            .BuildRequestExecutorAsync();
        var result3 = await executor3.ExecuteAsync("{ test {isBar isBaz isFoo }}");

        new StringBuilder()
            .AppendLine("List:")
            .AppendLine(result1.ToJson())
            .AppendLine("NestedList:")
            .AppendLine(result2.ToJson())
            .AppendLine("Null")
            .AppendLine(result3.ToJson())
            .ToString()
            .MatchSnapshot();
    }

    [Fact]
    public async Task Input_Should_WorkOnArguments()
    {
        FlagsEnum? result1 = null;
        var executor1 = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(
                x => x.Name("Query")
                    .Field("test")
                    .Type<StringType>()
                    .Argument("input", x => x.Type(typeof(FlagsEnum)))
                    .Resolve(
                        ctx =>
                        {
                            result1 = ctx.ArgumentValue<FlagsEnum>("input");

                            return "";
                        }))
            .ModifyOptions(x => x.EnableFlagEnums = true)
            .BuildRequestExecutorAsync();
        await executor1.ExecuteAsync("{ test(input: {isBar: true, isBaz: false, isFoo: false}) }");

        FlagsEnum? result2 = null;
        var executor2 = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(
                x => x.Name("Query")
                    .Field("test")
                    .Type<StringType>()
                    .Argument("input", x => x.Type(typeof(FlagsEnum)))
                    .Resolve(
                        ctx =>
                        {
                            result2 = ctx.ArgumentValue<FlagsEnum>("input");

                            return "";
                        }))
            .ModifyOptions(x => x.EnableFlagEnums = true)
            .BuildRequestExecutorAsync();
        await executor2.ExecuteAsync("{ test(input: {isBar: true, isBaz: false, isFoo: true}) }");

        Assert.Equal(result1, Bar);
        Assert.Equal(result2, Bar | FlagsEnum.Foo);
    }

    [Fact]
    public async Task Input_Should_WorkOnArguments_List()
    {
        FlagsEnum? result1 = null;
        var executor1 = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(
                x => x.Name("Query")
                    .Field("test")
                    .Type<StringType>()
                    .Argument("input", x => x.Type(typeof(List<FlagsEnum>)))
                    .Resolve(
                        ctx =>
                        {
                            result1 = ctx.ArgumentValue<List<FlagsEnum>>("input")[0];

                            return "";
                        }))
            .ModifyOptions(x => x.EnableFlagEnums = true)
            .BuildRequestExecutorAsync();
        await executor1.ExecuteAsync("{ test(input: {isBar: true, isBaz: false, isFoo: false}) }");

        FlagsEnum? result2 = null;
        var executor2 = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(
                x => x.Name("Query")
                    .Field("test")
                    .Type<StringType>()
                    .Argument("input", x => x.Type(typeof(List<List<FlagsEnum>>)))
                    .Resolve(
                        ctx =>
                        {
                            result2 = ctx.ArgumentValue<List<List<FlagsEnum>>>("input")[0][0];

                            return "";
                        }))
            .ModifyOptions(x => x.EnableFlagEnums = true)
            .BuildRequestExecutorAsync();
        await executor2.ExecuteAsync("{ test(input: {isBar: true, isBaz: false, isFoo: true}) }");

        Assert.Equal(result1, Bar);
        Assert.Equal(result2, Bar | FlagsEnum.Foo);
    }

    [Fact]
    public async Task Input_Should_WorkOnInput()
    {
        FlagsEnum? result1 = null;
        var executor1 = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(
                x => x.Name("Query")
                    .Field("test")
                    .Type<StringType>()
                    .Argument("input", x => x.Type<InputObjectType<SimpleInput>>())
                    .Resolve(
                        ctx =>
                        {
                            result1 = ctx.ArgumentValue<SimpleInput>("input").Single;

                            return "";
                        }))
            .ModifyOptions(x => x.EnableFlagEnums = true)
            .BuildRequestExecutorAsync();
        await executor1.ExecuteAsync(
            "{ test(input: {single: {isBar: true, isBaz: false, isFoo: true}}) }");

        Assert.Equal(result1, Bar | FlagsEnum.Foo);
    }

    [Fact]
    public async Task Input_Should_EmptySelection()
    {
        FlagsEnum? enumValue = null;
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(
                x => x.Name("Query")
                    .Field("test")
                    .Type<StringType>()
                    .Argument("input", x => x.Type(typeof(FlagsEnum)))
                    .Resolve(
                        ctx =>
                        {
                            enumValue = ctx.ArgumentValue<SimpleInput>("input").Single;

                            return "";
                        }))
            .ModifyOptions(x => x.EnableFlagEnums = true)
            .BuildRequestExecutorAsync();
        var result = await executor.ExecuteAsync("{ test(input: {}) }");

        Assert.Null(enumValue);
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task Input_Should_UnknownValue()
    {
        FlagsEnum? enumValue = null;
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(
                x => x.Name("Query")
                    .Field("test")
                    .Type<StringType>()
                    .Argument("input", x => x.Type(typeof(FlagsEnum)))
                    .Resolve(
                        ctx =>
                        {
                            enumValue = ctx.ArgumentValue<SimpleInput>("input").Single;

                            return "";
                        }))
            .AddDocumentFromString("extend input FlagsEnumFlagsInput { isAsd : Boolean }")
            .ModifyOptions(x => x.EnableFlagEnums = true)
            .BuildRequestExecutorAsync();
        var result = await executor.ExecuteAsync("{ test(input: {isAsd:true}) }");

        Assert.Null(enumValue);
        result.ToJson().MatchSnapshot();
    }

    [Theory]
    [InlineData(typeof(ByteEnum))]
    [InlineData(typeof(SByteEnum))]
    [InlineData(typeof(ShortEnum))]
    [InlineData(typeof(UShortEnum))]
    [InlineData(typeof(IntEnum))]
    [InlineData(typeof(UIntEnum))]
    [InlineData(typeof(LongEnum))]
    [InlineData(typeof(ULongEnum))]
    public async Task Input_Should_WorkOnArguments_DifferentEnumTypes(Type type)
    {
        object? result = null;
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(
                x => x.Name("Query")
                    .Field("test")
                    .Type<StringType>()
                    .Argument("input", x => x.Type(type))
                    .Resolve(
                        ctx =>
                        {
                            result = ctx.ArgumentValue<object>("input");

                            return "";
                        }))
            .ModifyOptions(x => x.EnableFlagEnums = true)
            .BuildRequestExecutorAsync();
        await executor.ExecuteAsync(
            "{ test(input: {isBar: true, isBaz: false, isFoo: true}) }");

        result.MatchSnapshot();
    }

    public class OutputQuery
    {
        public FlagsEnum Single() => Bar | FlagsEnum.Foo;

        public FlagsEnum[] List() => [Bar | FlagsEnum.Foo,];

        public FlagsEnum[][] NestedList() => [[Bar | FlagsEnum.Foo,],];

        public FlagsEnum? NullableSingle() => Bar | FlagsEnum.Foo;

        public FlagsEnum?[]? NullableList() => [Bar | FlagsEnum.Foo,];

        public FlagsEnum?[]?[]? NullableNestedList()
            => [[Bar | FlagsEnum.Foo,],];
    }

    [GraphQLDescription("This is the type desc")]
    [Flags]
    public enum FlagsWithDescription
    {
        [GraphQLDescription("Foo has a desc")] Foo = 1,
        [GraphQLDescription("Bar has a desc")] Bar = 2,
        [GraphQLDescription("Baz has a desc")] Baz = 3,
    }

    [InterfaceType()]
    public interface Interface
    {
        FlagsEnum Single();
    }

    public class Impl : Interface
    {
        public FlagsEnum Single()
        {
            throw new NotImplementedException();
        }
    }

    public class InputQuery
    {
        public FlagsEnum Loopback(FlagsEnum args) => args;

        public FlagsEnum Input(EnumInput input) => input.Single;
    }

    public class SimpleInput
    {
        public FlagsEnum Single { get; set; }
    }

    public class EnumInput
    {
        public FlagsEnum Single { get; set; }

        public FlagsEnum[] List { get; set; } = default!;

        public FlagsEnum[][] NestedList { get; set; } = default!;

        public FlagsEnum? NullableSingle { get; set; }

        public FlagsEnum?[]? NullableList { get; set; }

        public FlagsEnum?[]?[]? NullableNestedList { get; set; }
    }

    [Flags]
    public enum FlagsEnum
    {
        Foo = 1,
        Bar = 2,
        Baz = 4,
    }

    [Flags]
    public enum ByteEnum : byte
    {
        Foo = 0x1,
        Bar = 0x2,
        Baz = 0x4,
    }

    [Flags]
    public enum SByteEnum : sbyte
    {
        Foo = 0x1,
        Bar = 0x2,
        Baz = 0x4,
    }

    [Flags]
    public enum ShortEnum : short
    {
        Foo = 1,
        Bar = 2,
        Baz = 4,
    }

    [Flags]
    public enum UShortEnum : ushort
    {
        Foo = 1,
        Bar = 2,
        Baz = 4,
    }

    [Flags]
    public enum IntEnum : int
    {
        Foo = 1,
        Bar = 2,
        Baz = 4,
    }

    [Flags]
    public enum UIntEnum : uint
    {
        Foo = 1,
        Bar = 2,
        Baz = 4,
    }

    [Flags]
    public enum LongEnum : long
    {
        Foo = 1,
        Bar = 2,
        Baz = 4,
    }

    [Flags]
    public enum ULongEnum : ulong
    {
        Foo = 1,
        Bar = 2,
        Baz = 4,
    }
}
