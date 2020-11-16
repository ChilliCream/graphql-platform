using System.Linq;
using System.Reflection;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using Xunit;

namespace HotChocolate.Data.Filters
{
    public class FilterTypeAttributeTests
    {
        [Fact]
        public void GenericTypeDescriptorAttribute_Changes_Name()
        {
            // act
            ISchema schema = SchemaBuilder.New()
                .AddFiltering()
                .AddType<FilterInputType<FooGeneric>>()
                .ModifyOptions(o => o.StrictValidation = false)
                .Create();

            // assert
            Assert.Equal(
                GenericTypeFilterAttribute.TypeName,
                schema.GetType<FilterInputType<FooGeneric>>(
                    GenericTypeFilterAttribute.TypeName).TypeName().Value);
        }

        [Fact]
        public void FilterFieldAttribute_Changes_Name()
        {
            // act
            ISchema schema = SchemaBuilder.New()
                .AddFiltering()
                .AddType<FilterInputType<FooFields>>()
                .ModifyOptions(o => o.StrictValidation = false)
                .Create();

            // assert
            Assert.NotNull(
                schema.GetType<FilterInputType<FooFields>>("FooFieldsFilterInput")
                    .Fields
                    .FirstOrDefault(x => x.Name == FilterFieldAttributeTest.Field));
        }

        [GenericTypeFilterAttribute]
        public class FooGeneric
        {
            public string StringFilterTest { get; set; }
        }

        [FilterFieldAttributeTest]
        public class FooFields
        {
            [FilterFieldAttributeTest] public string Field { get; set; }
        }

        public class GenericTypeFilterAttribute
            : DescriptorAttribute
        {
            public static string TypeName { get; } = "ThisIsATest";

            protected internal override void TryConfigure(
                IDescriptorContext context,
                IDescriptor d,
                ICustomAttributeProvider element)
            {
                if (d is FilterInputTypeDescriptor<FooGeneric> descriptor)
                {
                    descriptor.Name(TypeName);
                }
            }
        }

        public class FilterFieldAttributeTest
            : DescriptorAttribute
        {
            public static string Field { get; } = "FieldField";

            protected internal override void TryConfigure(
                IDescriptorContext context,
                IDescriptor descriptor,
                ICustomAttributeProvider element)
            {
                if (descriptor is FilterFieldDescriptor filterFieldDescriptor)
                {
                    filterFieldDescriptor.Name(Field);
                }
            }
        }
    }
}
