using System;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Stitching.Schemas.SpecialCases
{
    public static class SpecialCasesSchemaFactory
    {
        public static ISchema Create()
        {
            return SchemaBuilder.New()
                .AddQueryType<QueryType>()
                .Create();
        }

        public static void ConfigureServices(IServiceCollection services)
        {
            services.AddGraphQL(Create());
        }
    }

    public class QueryType
        : ObjectType
    {
        protected override void Configure(IObjectTypeDescriptor descriptor)
        {
            descriptor.Field("custom_scalar")
                .Type<MyCustomScalarType>()
                .Argument("bar", a => a.Type<MyCustomScalarType>())
                .Resolver(ctx => ctx.Argument<MyCustomScalarValue>("bar"));

            descriptor.Field("custom_scalar_complex")
                .Type<MyCustomScalarType>()
                .Argument("bar", a => a.Type<CustomInputValueType>())
                .Resolver(ctx =>
                {
                    CustomInputValue input = ctx.Argument<CustomInputValue>("bar");

                    return new MyCustomScalarValue { Text = $"{input.From.Text}-{input.To.Text}" };
                });
        }
    }

    public class MyCustomScalarValue
    {
        public string Text { get; set; }
    }

    public class MyCustomScalarType : ScalarType<MyCustomScalarValue, StringValueNode>
    {
        public MyCustomScalarType() : base(nameof(MyCustomScalarValue))
        {
        }

        protected override MyCustomScalarValue ParseLiteral(StringValueNode literal)
        {
            return new MyCustomScalarValue {Text = literal.Value};
        }

        protected override StringValueNode ParseValue(MyCustomScalarValue value)
        {
            return new StringValueNode(null, value.Text, false);
        }

        public override bool TrySerialize(object value, out object serialized)
        {
            if (value is null)
            {
                serialized = null;
                return true;
            }

            if (value is MyCustomScalarValue s)
            {
                serialized=  s.Text;
                return true;
            }

            serialized = null;
            return false;
        }

        public override bool TryDeserialize(object serialized, out object value)
        {
            if (serialized is null)
            {
                value = null;
                return true;
            }

            if (serialized is string s)
            {
                value = new MyCustomScalarValue { Text = s };
                return true;
            }

            value = null;
            return false;
        }
    }

    public class CustomInputValue
    {
        public MyCustomScalarValue From { get; set; }

        public MyCustomScalarValue To { get; set; }
    }

    public class CustomInputValueType : InputObjectType<CustomInputValue>
    {
        protected override void Configure(IInputObjectTypeDescriptor<CustomInputValue> descriptor)
        {
            base.Configure(descriptor);

            descriptor.Field(i => i.From)
                .Type<NonNullType<MyCustomScalarType>>();

            descriptor.Field(i => i.To)
                .Type<NonNullType<MyCustomScalarType>>();
        }
    }
}
