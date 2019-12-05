using System.Linq;
using Xunit;

namespace HotChocolate.Types
{
    public class EnumTypeDescriptorAttributeTests
    {
        [Fact]
        public void Change_Value_Name_With_Attribute()
        {
            // act
            ISchema schema = SchemaBuilder.New()
                .AddEnumType<Enum1>()
                .ModifyOptions(o => o.StrictValidation = false)
                .Create();

            // assert
            EnumValue value = schema.GetType<EnumType>("Enum1").Values.First();
            Assert.Equal("ABC", value.Name);
        }

        [Fact]
        public void Change_Type_Name_With_Attribute()
        {
            // act
            ISchema schema = SchemaBuilder.New()
                .AddEnumType<Enum2>()
                .ModifyOptions(o => o.StrictValidation = false)
                .Create();

            // assert
            Assert.NotNull(schema.GetType<EnumType>("Abc"));
        }

        public enum Enum1
        {

            [RenameValue]
            Value1,
            Value2
        }

        public class RenameValueAttribute
            : EnumValueDescriptorAttribute
        {
            public override void OnConfigure(IEnumValueDescriptor descriptor)
            {
                descriptor.Name("ABC");
            }
        }

        [RenameType]
        public enum Enum2
        {

            Value1,
            Value2
        }

        public class RenameTypeAttribute
            : EnumTypeDescriptorAttribute
        {
            public override void OnConfigure(IEnumTypeDescriptor descriptor)
            {
                descriptor.Name("Abc");
            }
        }
    }
}
