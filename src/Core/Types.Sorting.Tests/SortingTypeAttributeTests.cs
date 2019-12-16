using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace HotChocolate.Types.Sorting
{

    public class SortTypeAttributeTests
        : TypeTestBase
    {
        [Fact]
        public void TypeDescriptorAttribute_Changes_Name()
        {
            // act
            ISchema schema = SchemaBuilder.New()
                .AddType<SortInputType<Foo>>()
                .ModifyOptions(o => o.StrictValidation = false)
                .Create();

            // assert
            Assert.Equal(
                FooTypeSortAttribute.TypeName,
                schema.GetType<SortInputType<Foo>>(FooTypeSortAttribute.TypeName).TypeName().Value);
        }

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

        [Fact]
        public void SortOperationObjectFieldAttribute_Changes_Name()
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
                        .FirstOrDefault(
                        x => x.Name == SortFieldAttributeTest.SortOperationObjectField));
        }

        [Fact]
        public void SortOperationObjectFieldFooAttribute_Changes_Name()
        {
            // act
            ISchema schema = SchemaBuilder.New()
                .AddType(new SortInputType<FooFields>(x => x.SortableObject(x => x.Bar)))
                .ModifyOptions(o => o.StrictValidation = false)
                .Create();

            // assert
            Assert.NotNull(
                schema.GetType<SortInputType<FooFields>>("FooFieldsSort")
                        .Fields
                        .FirstOrDefault(
                            x => x.Name == SortFieldAttributeTest.SortOperationObjectFieldFoo));
        }

        [FooTypeSortAttribute]
        public class Foo
        {
            public string StringSortTest { get; set; }
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
            public Foo Object { get; set; }
            [SortFieldAttributeTest]
            public Bar Bar { get; set; }
        }

        public class Bar
        {
            public string Baz { get; set; }
        }

        public class FooTypeSortAttribute
            : DescriptorAttribute
        {
            public static string TypeName { get; } = "ThisIsATest";

            protected override void TryConfigure(IDescriptor d)
            {
                if (d is SortInputTypeDescriptor descriptor)
                {
                    descriptor.Name(TypeName);
                }
            }
        }

        public class GenericTypeSortAttribute
            : DescriptorAttribute
        {
            public static string TypeName { get; } = "ThisIsATest";

            protected override void TryConfigure(IDescriptor d)
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
            public static string SortOperationField { get; } = "SortOperationField";
            public static string SortOperationObjectField { get; } = "SortOperationObjectField";
            public static string SortOperationObjectFieldFoo { get; } =
                "SortOperationObjectFieldFoo";

            protected override void TryConfigure(IDescriptor d)
            {
                if (d is SortOperationDescriptor sortOperationDescritor)
                {
                    sortOperationDescritor.Name(SortOperationField);
                }
                if (d is SortObjectOperationDescriptor sortOperationObject)
                {
                    sortOperationObject.Name(SortOperationObjectField);
                }
                if (d is SortObjectOperationDescriptor<Bar> sortOperationDescritorOfFoo)
                {
                    sortOperationDescritorOfFoo.Name(SortOperationObjectFieldFoo);
                }
            }
        }

    }
}
