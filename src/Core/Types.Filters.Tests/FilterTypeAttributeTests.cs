using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace HotChocolate.Types.Filters
{

    public class FilterTypeAttributeTests
        : TypeTestBase
    {
        [Fact]
        public void TypeDescriptorAttribute_Changes_Name()
        {
            // act
            ISchema schema = SchemaBuilder.New()
                .AddType<FilterInputType<Foo>>()
                .ModifyOptions(o => o.StrictValidation = false)
                .Create();

            // assert
            Assert.Equal(
                FooTypeFilterAttribute.TypeName,
                schema.GetType<FilterInputType<Foo>>(FooTypeFilterAttribute.TypeName).TypeName().Value);
        }

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
                schema.GetType<FilterInputType<FooGeneric>>(GenericTypeFilterAttribute.TypeName).TypeName().Value);
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
        public void FilterFieldArrayAttribute_Changes_Name()
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
                        .FirstOrDefault(x => x.Name == FilterFieldAttributeTest.ArrayField));
        }

        [Fact]
        public void FilterFieldObjectArrayAttribute_Changes_Name()
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
                        .FirstOrDefault(x => x.Name == FilterFieldAttributeTest.ObjectArrayField));
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

        [Fact]
        public void FilterFieldObjectAttribute_Changes_Name()
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
                        .FirstOrDefault(x => x.Name == FilterFieldAttributeTest.ObjectField));
        }


        [FooTypeFilterAttribute]
        public class Foo
        {
            public string StringFilterTest { get; set; }
        }

        [GenericTypeFilterAttribute]
        public class FooGeneric
        {
            public string StringFilterTest { get; set; }
        }

        public class FooFields
        {
            [FilterFieldAttributeTest]
            public string String { get; set; }
            [FilterFieldAttributeTest]
            public int Comparable { get; set; }
            [FilterFieldAttributeTest]
            public bool Boolean { get; set; }
            [FilterFieldAttributeTest]
            public Foo Object { get; set; }
            [FilterFieldAttributeTest]
            public int[] SingleArray { get; set; }
            [FilterFieldAttributeTest]
            public Foo[] ObjectArray { get; set; }
        }

        public class MyDirective
        {
        }

        public class FooTypeFilterAttribute
            : DescriptorAttribute
        {
            public static string TypeName { get; } = "ThisIsATest";

            protected override void TryConfigure(IDescriptor d)
            {
                if (d is FilterInputTypeDescriptor descriptor)
                {
                    descriptor.Name(TypeName);
                }
            }
        }

        public class GenericTypeFilterAttribute
            : DescriptorAttribute
        {
            public static string TypeName { get; } = "ThisIsATest";

            protected override void TryConfigure(IDescriptor d)
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
            public static string ObjectField { get; } = "ObjectField";
            public static string ArrayField { get; } = "ArrayField";
            public static string ObjectArrayField { get; } = "ObjectArrayField";
            public static string ComparableField { get; } = "ComparableField";
            public static string BooleanField { get; } = "BooleanField";

            protected override void TryConfigure(IDescriptor d)
            {
                if (d is IArrayFilterFieldDescriptor fieldArrayDescriptor)
                {
                    fieldArrayDescriptor.BindExplicitly().AllowAny().Name(ArrayField);
                }
                if (d is IArrayFilterFieldDescriptor<Foo> fieldObjectArrayDescriptor)
                {
                    fieldObjectArrayDescriptor.BindExplicitly().AllowAny().Name(ObjectArrayField);
                }
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
                if (d is IObjectFilterFieldDescriptor fieldObjectDescriptor)
                {
                    fieldObjectDescriptor
                        .BindExplicitly().AllowObject().Name(ObjectField);
                }
            }
        }

    }
}
