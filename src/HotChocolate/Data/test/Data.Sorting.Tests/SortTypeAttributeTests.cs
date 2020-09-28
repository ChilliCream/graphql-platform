using System.Linq;
using System.Reflection;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using Xunit;

namespace HotChocolate.Data.Sorting
{
    public class SortTypeAttributeTests
    {
        [Fact]
        public void GenericTypeDescriptorAttribute_Changes_Name()
        {
            // act
            ISchema schema = SchemaBuilder.New()
                .AddSorting()
                .AddType<SortInputType<FooGeneric>>()
                .ModifyOptions(o => o.StrictValidation = false)
                .Create();

            // assert
            Assert.Equal(
                GenericTypeSortAttribute.TypeName,
                schema.GetType<SortInputType<FooGeneric>>(
                        GenericTypeSortAttribute.TypeName)
                    .TypeName()
                    .Value);
        }

        [Fact]
        public void SortFieldAttribute_Changes_Name()
        {
            // act
            ISchema schema = SchemaBuilder.New()
                .AddSorting()
                .AddType<SortInputType<FooFields>>()
                .ModifyOptions(o => o.StrictValidation = false)
                .Create();

            // assert
            Assert.NotNull(
                schema.GetType<SortInputType<FooFields>>("FooFieldsSortInput")
                    .Fields
                    .FirstOrDefault(x => x.Name == SortFieldAttributeTest.Field));
        }

        [GenericTypeSortAttribute]
        public class FooGeneric
        {
            public string StringSortTest { get; set; } = default!;
        }

        [SortFieldAttributeTest]
        public class FooFields
        {
            [SortFieldAttributeTest] public string Field { get; set; } = default!;
        }

        public class GenericTypeSortAttribute
            : DescriptorAttribute
        {
            public static string TypeName { get; } = "ThisIsATest";

            protected override void TryConfigure(
                IDescriptorContext context,
                IDescriptor d,
                ICustomAttributeProvider element)
            {
                if (d is SortInputTypeDescriptor<FooGeneric> descriptor)
                {
                    descriptor.Name(TypeName);
                }
            }
        }

        public class SortFieldAttributeTest
            : DescriptorAttribute
        {
            public static string Field { get; } = "FieldField";

            protected override void TryConfigure(
                IDescriptorContext context,
                IDescriptor descriptor,
                ICustomAttributeProvider element)
            {
                if (descriptor is SortFieldDescriptor SortFieldDescriptor)
                {
                    SortFieldDescriptor.Name(Field);
                }
            }
        }
    }
}
