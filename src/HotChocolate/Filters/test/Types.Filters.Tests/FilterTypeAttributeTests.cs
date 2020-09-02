using System.Linq;
using System.Reflection;
using HotChocolate.Types.Descriptors;
using Xunit;

namespace HotChocolate.Types.Filters
{
    public class FilterTypeAttributeTests
        : TypeTestBase
    {
        [Fact]
        public void GenericTypeDescriptorAttribute_Changes_Name()
        {
            // act
            ISchema schema = SchemaBuilder.New()
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
        public void FilterFieldStringAttribute_Changes_Name()
        {
            // act
            ISchema schema = SchemaBuilder.New()
                .AddType<FilterInputType<FooFields>>()
                .ModifyOptions(o => o.StrictValidation = false)
                .Create();

            // assert
            Assert.NotNull(
                schema.GetType<FilterInputType<FooFields>>("FooFieldsFilter")
                        .Fields
                        .FirstOrDefault(x => x.Name == FilterFieldAttributeTest.StringField));
        }

        [Fact]
        public void FilterFieldComparableAttribute_Changes_Name()
        {
            // act
            ISchema schema = SchemaBuilder.New()
                .AddType<FilterInputType<FooFields>>()
                .ModifyOptions(o => o.StrictValidation = false)
                .Create();

            // assert
            Assert.NotNull(
                schema.GetType<FilterInputType<FooFields>>("FooFieldsFilter")
                        .Fields
                        .FirstOrDefault(x => x.Name == FilterFieldAttributeTest.ComparableField));
        }

        [Fact]
        public void FilterFieldBooleanAttribute_Changes_Name()
        {
            // act
            ISchema schema = SchemaBuilder.New()
                .AddType<FilterInputType<FooFields>>()
                .ModifyOptions(o => o.StrictValidation = false)
                .Create();

            // assert
            Assert.NotNull(
                schema.GetType<FilterInputType<FooFields>>("FooFieldsFilter")
                        .Fields
                        .FirstOrDefault(x => x.Name == FilterFieldAttributeTest.BooleanField));
        }

        [GenericTypeFilterAttribute]
        public class FooGeneric
        {
            public string StringFilterTest { get; set; }
        }

        [FilterFieldAttributeTest]
        public class FooFields
        {
            [FilterFieldAttributeTest]
            public string String { get; set; }
            [FilterFieldAttributeTest]
            public int Comparable { get; set; }
            [FilterFieldAttributeTest]
            public bool Boolean { get; set; }
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
            public static string StringField { get; } = "StringField";
            public static string ComparableField { get; } = "ComparableField";
            public static string BooleanField { get; } = "BooleanField";

            protected internal override void TryConfigure(
                IDescriptorContext context,
                IDescriptor d,
                ICustomAttributeProvider element)
            {
                if (d is IComparableFilterFieldDescriptor fieldComparableDescriptor)
                {
                    fieldComparableDescriptor
                        .BindFiltersExplicitly().AllowEquals().Name(ComparableField);
                }
                if (d is IBooleanFilterFieldDescriptor fieldBooleanDescriptor)
                {
                    fieldBooleanDescriptor
                        .BindFiltersExplicitly().AllowEquals().Name(BooleanField);
                }
                if (d is IStringFilterFieldDescriptor fieldStringDescriptor)
                {
                    fieldStringDescriptor
                        .BindFiltersExplicitly().AllowEquals().Name(StringField);
                }
            }
        }

    }
}
