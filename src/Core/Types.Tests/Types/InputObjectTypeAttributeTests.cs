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

        public class Object1
        {
            [RenameField]
            public string Foo { get; set; }
        }

        public class RenameFieldAttribute
            : InputFieldDescriptorAttribute
        {
            public override void OnConfigure(IInputFieldDescriptor descriptor)
            {
                descriptor.Name("bar");
            }
        }

        [RenameType]
        public class Object2
        {
            public string Foo { get; set; }
        }

        public class RenameTypeAttribute
            : InputObjectTypeDescriptorAttribute
        {
            public override void OnConfigure(IInputObjectTypeDescriptor descriptor)
            {
                descriptor.Name("Bar");
            }
        }
    }
}
