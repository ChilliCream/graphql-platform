using System;
using System.Reflection;
using HotChocolate.Types.Descriptors;
using Xunit;

namespace HotChocolate.Types.Sorting
{
    public class SortFieldDescriptorTests
        : DescriptorTestBase
    {
        [Fact]
        public void Ctor_PropertyNull_ShouldThrowArgumentNullException()
        {
            // act
            SortFieldDescriptor Create()
                => new SortFieldDescriptor(Context, null);

            // assert
            Assert.Throws<ArgumentNullException>((Func<SortFieldDescriptor>) Create);
        }

        [Fact]
        public void Ctor_WithProperty_ShouldInitDefinition()
        {
            // arrange
            PropertyInfo property = typeof(Foo).GetProperty(nameof(Foo.Bar));

            // act
            var descriptor = new SortFieldDescriptorInternal(Context, property);

            // assert
            Assert.Equal(
                descriptor.DefinitionAccessor.Name,
                Context.Naming.GetMemberName(
                    property,
                    MemberKind.InputObjectField));
            Assert.Same(
                descriptor.DefinitionAccessor.Property,
                property);
            Assert.Equal(
                descriptor.DefinitionAccessor.Type,
                new ClrTypeReference(typeof(string), TypeContext.Input));
        }

        [Fact]
        public void Ignore_ShouldSetIgnoreFlag()
        {
            // arrange
            PropertyInfo property = typeof(Foo).GetProperty(nameof(Foo.Bar));
            var descriptor = new SortFieldDescriptorInternal(Context, property);

            // act
            descriptor.Ignore();

            // assert
            Assert.True(descriptor.DefinitionAccessor.Ignore);
        }

        [Fact]
        public void Name_ShouldSetName()
        {
            // arrange
            PropertyInfo property = typeof(Foo).GetProperty(nameof(Foo.Bar));
            var descriptor = new SortFieldDescriptorInternal(Context, property);

            // act
            descriptor.Name("qux");

            // assert
            Assert.Equal("qux", descriptor.DefinitionAccessor.Name);
        }

        private class SortFieldDescriptorInternal : SortFieldDescriptor
        {
            public SortFieldDescriptorInternal(
                IDescriptorContext context,
                PropertyInfo property) : base(context, property)
            {
            }

            public SortFieldDefinition DefinitionAccessor => Definition;
        }

        private class Foo
        {
            public string Bar { get; set; }
        }
    }
}
