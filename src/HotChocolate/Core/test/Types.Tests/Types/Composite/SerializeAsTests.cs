using System.Text.Json;
using HotChocolate.Execution;
using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types.Composite;

public static class SerializeAsTests
{
    [Fact]
    public static async Task SerializeAs_Is_Not_Added()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query>()
                .BuildSchemaAsync();

        schema.MatchSnapshot();
    }

    [Fact]
    public static async Task SerializeAs_Is_Added()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query>()
                .ModifyOptions(o => o.ApplySerializeAsToScalars = true)
                .BuildSchemaAsync();

        schema.MatchSnapshot();
    }

    public class Query
    {
        [GraphQLType<NonNullType<CustomString1>>]
        public string GetFoo() => "foo";

        [GraphQLType<NonNullType<CustomString2>>]
        public string GetBar() => "foo";

        [GraphQLType<NonNullType<CustomString3>>]
        public string GetBaz() => "foo";
    }

    public class CustomString1 : ScalarType<string>
    {
        public CustomString1() : base("Custom1")
        {
        }

        public override ScalarSerializationType SerializationType => ScalarSerializationType.String;

        public override object CoerceInputLiteral(IValueNode valueLiteral)
            => throw new NotImplementedException();

        public override object CoerceInputValue(JsonElement inputValue, IFeatureProvider context)
            => throw new NotImplementedException();

        protected override void OnCoerceOutputValue(string runtimeValue, ResultElement resultValue)
            => throw new NotImplementedException();

        protected override IValueNode OnValueToLiteral(string runtimeValue)
            => throw new NotImplementedException();
    }

    public class CustomString2 : ScalarType
    {
        public CustomString2() : base("Custom2")
        {
        }

        public override Type RuntimeType => typeof(object);

        public override ScalarSerializationType SerializationType => ScalarSerializationType.Any;

        public override object CoerceInputLiteral(IValueNode valueLiteral)
            => throw new NotImplementedException();

        public override object CoerceInputValue(JsonElement inputValue, IFeatureProvider context)
            => throw new NotImplementedException();

        public override void CoerceOutputValue(object runtimeValue, ResultElement resultValue)
            => throw new NotImplementedException();

        public override IValueNode ValueToLiteral(object runtimeValue)
            => throw new NotImplementedException();
    }

    public class CustomString3 : ScalarType<string>
    {
        public CustomString3() : base("Custom3")
        {
            Pattern = "\\b\\d{3}\\b";
        }

        public override ScalarSerializationType SerializationType => ScalarSerializationType.String;

        public override object CoerceInputLiteral(IValueNode valueLiteral)
            => throw new NotImplementedException();

        public override object CoerceInputValue(JsonElement inputValue, IFeatureProvider context)
            => throw new NotImplementedException();

        protected override void OnCoerceOutputValue(string runtimeValue, ResultElement resultValue)
            => throw new NotImplementedException();

        protected override IValueNode OnValueToLiteral(string runtimeValue)
            => throw new NotImplementedException();
    }
}
