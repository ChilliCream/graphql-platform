using HotChocolate.Execution;
using HotChocolate.Tests;

namespace HotChocolate.Types;

public class InputValueFormatterTests
{
    [Fact]
    public async Task Add_Input_Formatter_To_Argument()
    {
        await SchemaBuilder.New()
            .AddQueryType<QueryType>()
            .Create()
            .MakeExecutable()
            .ExecuteAsync("{ one(arg: \"abc\") }")
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task Add_Chained_Input_Formatter_To_Argument()
    {
        await SchemaBuilder.New()
            .AddQueryType<QueryType>()
            .Create()
            .MakeExecutable()
            .ExecuteAsync("{ two(arg: \"abc\") }")
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task Add_Input_Formatter_To_Field()
    {
        await SchemaBuilder.New()
            .AddQueryType<QueryType>()
            .Create()
            .MakeExecutable()
            .ExecuteAsync("{ one_input(arg: { bar: \"abc\" }) }")
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task Add_Chained_Input_Formatter_To_Field()
    {
        await SchemaBuilder.New()
            .AddQueryType<QueryType>()
            .Create()
            .MakeExecutable()
            .ExecuteAsync("{ two_input(arg: { baz: \"abc\" }) }")
            .MatchSnapshotAsync();
    }

    public class QueryType : ObjectType
    {
        protected override void Configure(IObjectTypeDescriptor descriptor)
        {
            descriptor.Name("Query");

            descriptor.Field("one")
                .Argument("arg", a => a.Type<StringType>()
                    .Extend()
                    .OnBeforeCreate(d => d.Formatters.Add(new UpperCaseInputValueFormatter())))
                .Type<StringType>()
                .Resolve(c => c.ArgumentValue<string>("arg"));

            descriptor.Field("two")
                .Argument("arg", a => a.Type<StringType>()
                    .Extend()
                    .OnBeforeCreate(d =>
                    {
                        d.Formatters.Add(new UpperCaseInputValueFormatter());
                        d.Formatters.Add(new AddTwoInputValueFormatter());
                    }))
                .Type<StringType>()
                .Resolve(c => c.ArgumentValue<string>("arg"));

            descriptor.Field("one_input")
                .Argument("arg", a => a.Type<FooInputType>())
                .Type<StringType>()
                .Resolve(c => c.ArgumentValue<Foo>("arg").Bar);

            descriptor.Field("two_input")
                .Argument("arg", a => a.Type<FooInputType>())
                .Type<StringType>()
                .Resolve(c => c.ArgumentValue<Foo>("arg").Baz);
        }
    }

    public class FooInputType : InputObjectType<Foo>
    {
        protected override void Configure(IInputObjectTypeDescriptor<Foo> descriptor)
        {
            descriptor.Field(t => t.Bar)
                .Extend()
                .OnBeforeCreate(d => d.Formatters.Add(new UpperCaseInputValueFormatter()));

            descriptor.Field(t => t.Baz)
                .Extend()
                .OnBeforeCreate(d =>
                {
                    d.Formatters.Add(new UpperCaseInputValueFormatter());
                    d.Formatters.Add(new AddTwoInputValueFormatter());
                });
        }
    }

    public class Foo
    {
        public string Bar { get; set; }

        public string Baz { get; set; }
    }

    public class UpperCaseInputValueFormatter : IInputValueFormatter
    {
        public object Format(object originalValue)
        {
            return originalValue is string s ? s.ToUpperInvariant() : originalValue;
        }
    }

    public class AddTwoInputValueFormatter : IInputValueFormatter
    {
        public object Format(object originalValue)
        {
            return originalValue is string s ? s + "2" : originalValue;
        }
    }
}
