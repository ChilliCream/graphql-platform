using System;
using Xunit;

#nullable enable

namespace HotChocolate.Types.Pagination
{
    public class PagingObjectFieldDescriptorExtensionsTests
    {
        [Fact]
        public static void ObjectFieldDescriptor_UseOffsetPaging_Descriptor_Is_Null()
        {
            Assert.Throws<ArgumentNullException>(
                () => PagingObjectFieldDescriptorExtensions.UsePaging(
                    default(IObjectFieldDescriptor)!));
        }

        [Fact]
        public static void ObjectFieldDescriptor_AddOffsetPagingArguments_Descriptor_Is_Null()
        {
            Assert.Throws<ArgumentNullException>(
                () => PagingObjectFieldDescriptorExtensions.AddPagingArguments(
                    default(IObjectFieldDescriptor)!));
        }

        [Fact]
        public static void InterfaceFieldDescriptor_UseOffsetPaging_Descriptor_Is_Null()
        {
            Assert.Throws<ArgumentNullException>(
                () => PagingObjectFieldDescriptorExtensions.UsePaging(
                    default(IInterfaceFieldDescriptor)!));
        }

        [Fact]
        public static void InterfaceFieldDescriptor_AddOffsetPagingArguments_Descriptor_Is_Null()
        {
            Assert.Throws<ArgumentNullException>(
                () => PagingObjectFieldDescriptorExtensions.AddPagingArguments(
                    default(IInterfaceFieldDescriptor)!));
        }
    }
}