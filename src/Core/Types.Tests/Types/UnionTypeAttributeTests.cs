using Xunit;

#nullable enable

namespace HotChocolate.Types
{
    public class UnionTypeAttributeTests
    {
        [Fact]
        public void SetName_Union_Interface()
        {
            // act
            ISchema schema = SchemaBuilder.New()
                .AddUnionType<IUnion1>()
                .AddType<Foo>()
                .ModifyOptions(o => o.StrictValidation = false)
                .Create();

            // assert
            Assert.NotNull(schema.GetType<UnionType>("Abc"));
        }

        [SetName]
        public interface IUnion1 { }

        public class Foo : IUnion1 { }

        public class SetNameAttribute : UnionTypeDescriptorAttribute
        {
            public override void OnConfigure(IUnionTypeDescriptor descriptor)
            {
                descriptor.Name("Abc");
            }
        }
    }
}
