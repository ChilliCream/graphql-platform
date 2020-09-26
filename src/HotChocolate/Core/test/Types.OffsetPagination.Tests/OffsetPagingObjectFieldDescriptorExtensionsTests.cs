using System;
using Xunit;

#nullable enable

namespace HotChocolate.Types.Pagination
{
    public class OffsetPagingObjectFieldDescriptorExtensionsTests
    {
        [Fact]
        public static void ObjectFieldDescriptor_UseOffsetPaging_Descriptor_Is_Null()
        {
            Assert.Throws<ArgumentNullException>(
                () => OffsetPagingObjectFieldDescriptorExtensions.UseOffsetPaging(
                    default(IObjectFieldDescriptor)!));
        }

        [Fact]
        public static void ObjectFieldDescriptor_AddOffsetPagingArguments_Descriptor_Is_Null()
        {
            Assert.Throws<ArgumentNullException>(
                () => OffsetPagingObjectFieldDescriptorExtensions.AddOffsetPagingArguments(
                    default(IObjectFieldDescriptor)!));
        }

        [Fact]
        public static void InterfaceFieldDescriptor_UseOffsetPaging_Descriptor_Is_Null()
        {
            Assert.Throws<ArgumentNullException>(
                () => OffsetPagingObjectFieldDescriptorExtensions.UseOffsetPaging(
                    default(IInterfaceFieldDescriptor)!));
        }

        [Fact]
        public static void InterfaceFieldDescriptor_AddOffsetPagingArguments_Descriptor_Is_Null()
        {
            Assert.Throws<ArgumentNullException>(
                () => OffsetPagingObjectFieldDescriptorExtensions.AddOffsetPagingArguments(
                    default(IInterfaceFieldDescriptor)!));
        }
    }
}