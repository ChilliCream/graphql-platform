using System.Text.Json;
using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Text.Json;
using HotChocolate.Types;

namespace HotChocolate.Execution;

public class ScalarExecutionErrorTests
{
    [Fact]
    public async Task OutputType_ClrValue_CannotBeConverted()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddQueryType<QueryType>()
            .Create();

        var executor = schema.MakeExecutable();

        // act
        var result = await executor.ExecuteAsync(
            "{ stringToName(name: \"  \") }");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task OutputType_ClrValue_CannotBeParsed()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddQueryType<QueryType>()
            .Create();

        var executor = schema.MakeExecutable();

        // act
        var result = await executor.ExecuteAsync(
            "{ stringToFoo(name: \"  \") }");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task InputType_Literal_CannotBeParsed()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddQueryType<QueryType>()
            .Create();

        var executor = schema.MakeExecutable();

        // act
        var result = await executor.ExecuteAsync(
            "{ nameToString(name: \"  \") }");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task InputType_Variable_CannotBeDeserialized()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddQueryType<QueryType>()
            .Create();

        var executor = schema.MakeExecutable();

        // act
        var result = await executor.ExecuteAsync(
            "query a($a: Foo) { fooToString(name: $a) }",
            new Dictionary<string, object?>
            {
                {"a", " "}
            });

        // assert
        result.MatchSnapshot();
    }

    public class Query
    {
        public string StringToName(string name) => name;

        public string NameToString(string name) => name;

        public string StringToFoo(string name) => name;

        public string FooToString(string name) => name;
    }

    public class QueryType : ObjectType<Query>
    {
        protected override void Configure(
            IObjectTypeDescriptor<Query> descriptor)
        {
            descriptor.Field(t => t.StringToName(null!))
                .Argument("name", a => a.Type<StringType>())
                .Type<NameType>();

            descriptor.Field(t => t.NameToString(null!))
                .Argument("name", a => a.Type<NameType>())
                .Type<StringType>();

            descriptor.Field(t => t.StringToFoo(null!))
                .Argument("name", a => a.Type<StringType>())
                .Type<FooType>();

            descriptor.Field(t => t.FooToString(null!))
                .Argument("name", a => a.Type<FooType>())
                .Type<StringType>();
        }
    }

    public class FooType : ScalarType
    {
        public FooType() : base("Foo")
        {
        }

        public override Type RuntimeType => typeof(string);

        public override ScalarSerializationType SerializationType => ScalarSerializationType.String;

        public override object CoerceInputLiteral(IValueNode literal)
        {
            if (literal is StringValueNode { Value: "a" })
            {
                return "a";
            }

            throw new LeafCoercionException("StringValue is not a.", this);
        }

        public override object CoerceInputValue(JsonElement inputValue, IFeatureProvider context)
        {
            if (inputValue.ValueKind is JsonValueKind.String)
            {
                var value = inputValue.GetString()!;
                if (value is "a")
                {
                    return value;
                }
            }

            throw new LeafCoercionException("StringValue is not a.", this);
        }

        public override void CoerceOutputValue(object runtimeValue, ResultElement resultValue)
        {
            if (runtimeValue is string s && s is "a")
            {
                resultValue.SetStringValue(s);
            }

            throw new LeafCoercionException("StringValue is not a.", this);
        }

        public override IValueNode ValueToLiteral(object runtimeValue)
        {
            if (runtimeValue is string s && s is "a")
            {
                return new StringValueNode(s);
            }

            throw new LeafCoercionException("StringValue is not a.", this);
        }
    }
}

public sealed class NameType : ScalarType<string, StringValueNode>
{
    public NameType() : base("Name", bind: BindingBehavior.Implicit)
    {
    }

    protected override string OnCoerceInputLiteral(StringValueNode valueLiteral)
    {
        if (string.IsNullOrWhiteSpace(valueLiteral.Value))
        {
            throw new LeafCoercionException("Not a valid name.", this);
        }

        return valueLiteral.Value;
    }

    protected override string OnCoerceInputValue(JsonElement inputValue, IFeatureProvider context)
    {
        var s = inputValue.GetString();

        if (string.IsNullOrWhiteSpace(s))
        {
            throw new LeafCoercionException("Not a valid name.", this);
        }

        return s;
    }

    protected override void OnCoerceOutputValue(string runtimeValue, ResultElement resultValue)
    {
        if (string.IsNullOrWhiteSpace(runtimeValue))
        {
            throw new LeafCoercionException("Not a valid name.", this);
        }

        resultValue.SetStringValue(runtimeValue);
    }

    protected override StringValueNode OnValueToLiteral(string runtimeValue)
        => new(runtimeValue);
}
