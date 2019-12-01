using System;
using Xunit;

#nullable enable

namespace HotChocolate.Types
{
    public class ObjectTypeAttributeTests
        : TypeTestBase
    {
        [Fact]
        public void ArgumentDescriptorAttribute_Changes_DefaultValue()
        {
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<Object1>()
                .Create();

            // assert
            Assert.Equal(
                "abc",
                schema.QueryType.Fields["field"].Arguments["argument"].DefaultValue.Value);
        }

        [Fact]
        public void ObjectFieldDescriptorAttribute_Adds_ContextData()
        {
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<Object2>()
                .Create();

            // assert
            Assert.Equal(
                "def",
                schema.QueryType.Fields["field"].ContextData["abc"]);
        }

        [Fact]
        public void ObjectFieldDescriptorAttribute_Updated_FieldDefinition()
        {
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<Object2>(d =>
                    d.Field<string>(t => t.GetField()).Name("foo"))
                .Create();

            // assert
            Assert.Equal(
                "def",
                schema.QueryType.Fields["foo"].ContextData["abc"]);
        }

        [Fact]
        public void ObjectTypeDescriptorAttribute_Add_FieldDefinition()
        {
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<Object3>()
                .Create();

            // assert
            Assert.True(schema.QueryType.Fields.ContainsField("abc"));
        }

        [Fact]
        public void ObjectTypeDescriptorAttribute_Add_FieldDefinition_2()
        {
            // act
            ISchema schema = SchemaBuilder.New()
                .AddObjectType<Object3>()
                .ModifyOptions(o => o.StrictValidation = false)
                .Create();

            // assert
            Assert.True(schema.GetType<ObjectType>("Object3").Fields.ContainsField("abc"));
        }

        public class Object1
        {
            public string GetField([ArgumentDefaultValue("abc")]string argument)
            {
                throw new NotImplementedException();
            }
        }

        public class ArgumentDefaultValueAttribute
            : ArgumentDescriptorAttribute
        {
            public ArgumentDefaultValueAttribute(object defaultValue)
            {
                DefaultValue = defaultValue;
            }

            public object DefaultValue { get; }

            public override void OnConfigure(IArgumentDescriptor descriptor)
            {
                descriptor.DefaultValue(DefaultValue);
            }
        }

        public class Object2
        {
            [PropertyAddContextData]
            public string GetField()
            {
                throw new NotImplementedException();
            }
        }

        public class PropertyAddContextDataAttribute
            : ObjectFieldDescriptorAttribute
        {
            public override void OnConfigure(IObjectFieldDescriptor descriptor)
            {
                descriptor.Extend().OnBeforeCompletion(
                    (c, d) => d.ContextData.Add("abc", "def"));
            }
        }

        [ObjectAddField]
        public class Object3
        {
            public string GetField()
            {
                throw new NotImplementedException();
            }
        }

        public class ObjectAddFieldAttribute
            : ObjectTypeDescriptorAttribute
        {
            public override void OnConfigure(IObjectTypeDescriptor descriptor)
            {
                descriptor.Field("abc").Resolver<string>("def");
            }
        }
    }
}
