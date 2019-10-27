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
                .Resolver(ctx => ctx.Argument<string>("bar"));
        }
    }

    public class MyCustomScalarType
        : ScalarType
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StringType"/> class.
        /// </summary>
        public MyCustomScalarType()
            : base("Custom")
        {
        }

        public override Type ClrType => typeof(string);

        public override bool IsInstanceOfType(IValueNode literal)
        {
            if (literal == null)
            {
                throw new ArgumentNullException(nameof(literal));
            }

            return literal is StringValueNode
                || literal is NullValueNode;
        }

        public override object ParseLiteral(IValueNode literal)
        {
            if (literal == null)
            {
                throw new ArgumentNullException(nameof(literal));
            }

            if (literal is StringValueNode stringLiteral)
            {
                return stringLiteral.Value;
            }

            if (literal is NullValueNode)
            {
                return null;
            }

            throw new ScalarSerializationException("some text");
        }

        public override IValueNode ParseValue(object value)
        {
            if (value == null)
            {
                return new NullValueNode(null);
            }

            if (value is string s)
            {
                return new StringValueNode(null, s, false);
            }

            throw new ScalarSerializationException("some text");
        }

        public override bool TrySerialize(object value, out object serialized)
        {
            if (value == null)
            {
                serialized = null;
                return true;
            }

            if (value is string s)
            {
                serialized =  s;
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
                value = s;
                return true;
            }

            value = null;
            return false;
        }
    }
}
