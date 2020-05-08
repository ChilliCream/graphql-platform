using System;
using HotChocolate.Types.Descriptors;
using Snapshooter.Xunit;
using Xunit;

#nullable enable

namespace HotChocolate.Types
{
    public class InputUnionTypeAttributeTests
    {
        [Fact]
        public void SetName_InputUnion_Interface()
        {
            // act
            ISchema schema = SchemaBuilder.New()
                .AddInputUnionType<IInputUnion1>()
                .AddInputObjectType<Foo>()
                .ModifyOptions(o => o.StrictValidation = false)
                .Create();

            // assert
            Assert.NotNull(schema.GetType<InputUnionType>("Abc"));
        }

        [Fact]
        public void InputUnionTypeAttribute_Infer_InputUnion()
        {
            // act
            ISchema schema = SchemaBuilder.New()
                .AddType<IInputUnion2>()
                .AddInputObjectType<InputUnion2Type1>()
                .ModifyOptions(o => o.StrictValidation = false)
                .Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        [SetName]
        public interface IInputUnion1 { }

        public class Foo : IInputUnion1
        {
            public string? Property { get; set; }
        }

        public class SetNameAttribute : InputUnionTypeDescriptorAttribute
        {
            public override void OnConfigure(
                IDescriptorContext context,
                IInputUnionTypeDescriptor descriptor,
                Type type)
            {
                descriptor.Name("Abc");
            }
        }

        [InputUnionType(Name = "InputUnion")]
        public interface IInputUnion2 { }

        [InputObjectType(Name = "Type")]
        public class InputUnion2Type1 : IInputUnion2
        {
            public string? Property { get; set; }
        }
    }
}
