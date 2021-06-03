using System;
using System.Linq;
using System.Reflection;
using HotChocolate.Types.Descriptors;
using Xunit;

namespace HotChocolate.Types.Sorting
{
    [Obsolete]
    public class SortTypeAttributeTests
        : TypeTestBase
    {
        [Fact]
        public void GenericTypeDescriptorAttribute_Changes_Name()
        {
            // act
            ISchema schema = SchemaBuilder.New()
                .AddType<SortInputType<FooGeneric>>()
                .ModifyOptions(o => o.StrictValidation = false)
                .Create();

            // assert
            Assert.Equal(
                GenericTypeSortAttribute.TypeName,
                schema.GetType<SortInputType<FooGeneric>>(
                    GenericTypeSortAttribute.TypeName).TypeName().Value);
        }

        [Fact]
        public void SortOperationFieldAttribute_Changes_Name()
        {
            // act
            ISchema schema = SchemaBuilder.New()
                .AddType<SortInputType<FooFields>>()
                .ModifyOptions(o => o.StrictValidation = false)
                .Create();

            // assert
            Assert.NotNull(
                schema.GetType<SortInputType<FooFields>>("FooFieldsSort")
                        .Fields
                        .FirstOrDefault(x => x.Name == SortFieldAttributeTest.SortOperationField));
        }

        [GenericTypeSortAttribute]
        public class FooGeneric
        {
            public string StringSortTest { get; set; }
        }

        [SortFieldAttributeTest]
        public class FooFields
        {
            [SortFieldAttributeTest]
            public string String { get; set; }
            [SortFieldAttributeTest]
            public Bar Bar { get; set; }
        }

        public class Bar
        {
            public string Baz { get; set; }
        }

        public class GenericTypeSortAttribute
            : DescriptorAttribute
        {
            public static string TypeName { get; } = "ThisIsATest";

            protected internal override void TryConfigure(
                IDescriptorContext context,
                IDescriptor d,
                ICustomAttributeProvider element)
            {
                {
                    if (d is SortInputTypeDescriptor<FooGeneric> descriptor)
                    {
                        descriptor.Name(TypeName);
                    }
                }
            }
        }

        public class SortFieldAttributeTest
            : DescriptorAttribute
        {
            public static string SortOperationField { get; } = "SortOperationField";

            protected internal override void TryConfigure(
                IDescriptorContext context,
                IDescriptor d,
                ICustomAttributeProvider element)
            {
                if (d is SortOperationDescriptor sortOperationDescritor)
                {
                    sortOperationDescritor.Name(SortOperationField);
                }
            }
        }

    }
}
