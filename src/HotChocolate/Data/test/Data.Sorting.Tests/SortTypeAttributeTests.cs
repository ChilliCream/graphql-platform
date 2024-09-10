using System.Reflection;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using static HotChocolate.Data.Sorting.SortTypeAttributeTests.GenericTypeSortAttribute;

namespace HotChocolate.Data.Sorting;

public class SortTypeAttributeTests
{
    [Fact]
    public void GenericTypeDescriptorAttribute_Changes_Name()
    {
        // act
        var schema = SchemaBuilder.New()
            .AddSorting()
            .AddType<SortInputType<FooGeneric>>()
            .ModifyOptions(o => o.StrictValidation = false)
            .Create();

        // assert
        Assert.Equal(
            TypeName,
            schema.GetType<SortInputType<FooGeneric>>(TypeName).TypeName());
    }

    [Fact]
    public void SortFieldAttribute_Changes_Name()
    {
        // act
        var schema = SchemaBuilder.New()
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

    [GenericTypeSort]
    public class FooGeneric
    {
        public string StringSortTest { get; set; } = default!;
    }

    [SortFieldAttributeTest]
    public class FooFields
    {
        [SortFieldAttributeTest] public string Field { get; set; } = default!;
    }

    [AttributeUsageAttribute(AttributeTargets.Class, AllowMultiple = false)]
    public class GenericTypeSortAttribute : DescriptorAttribute
    {
        public static string TypeName { get; } = "ThisIsATest";

        protected internal override void TryConfigure(
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

        protected internal override void TryConfigure(
            IDescriptorContext context,
            IDescriptor descriptor,
            ICustomAttributeProvider element)
        {
            if (descriptor is SortFieldDescriptor sortFieldDescriptor)
            {
                sortFieldDescriptor.Name(Field);
            }
        }
    }
}
