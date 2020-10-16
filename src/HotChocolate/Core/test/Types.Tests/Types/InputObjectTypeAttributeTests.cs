using System;
using System.Reflection;
using HotChocolate.Types.Descriptors;
using Xunit;

namespace HotChocolate.Types
{
    public class InputObjectTypeAttributeTests
    {
        [Fact]
        public void Change_Field_Name_With_Attribute()
        {
            // act
            ISchema schema = SchemaBuilder.New()
                .AddInputObjectType<Object1>()
                .ModifyOptions(o => o.StrictValidation = false)
                .Create();

            // assert
            Assert.True(
                schema.GetType<InputObjectType>("Object1Input")
                    .Fields
                    .ContainsField("bar"));
        }

        [Fact]
        public void Change_InputObjectType_Name_With_Attribute()
        {
            // act
            ISchema schema = SchemaBuilder.New()
                .AddInputObjectType<Object2>()
                .ModifyOptions(o => o.StrictValidation = false)
                .Create();

            // assert
            Assert.True(
                schema.GetType<InputObjectType>("Bar")
                    .Fields
                    .ContainsField("foo"));
        }

        [Fact]
        public void Annotated_Struct1_With_InputObjectTypeAttribute()
        {
            // act
            ISchema schema = SchemaBuilder.New()
                .AddInputObjectType<Struct1>()
                .ModifyOptions(o => o.StrictValidation = false)
                .Create();

            // assert
            Assert.True(
                schema.GetType<InputObjectType>("Foo")
                    .Fields
                    .ContainsField("foo"));
        }

        public class Object1
        {
            [RenameField]
            public string Foo { get; set; }
        }

        public class RenameFieldAttribute
            : InputFieldDescriptorAttribute
        {
            public override void OnConfigure(
                IDescriptorContext context,
                IInputFieldDescriptor descriptor,
                MemberInfo member)
            {
                descriptor.Name("bar");
            }
        }

        [RenameType]
        public class Object2
        {
            public string Foo { get; set; }
        }

        [InputObjectType(Name = "Foo")]
        public struct Struct1
        {
            public string Foo { get; set; }
        }

        public class RenameTypeAttribute
            : InputObjectTypeDescriptorAttribute
        {
            public override void OnConfigure(
                IDescriptorContext context,
                IInputObjectTypeDescriptor descriptor,
                Type type)
            {
                descriptor.Name("Bar");
            }
        }
    }
}
