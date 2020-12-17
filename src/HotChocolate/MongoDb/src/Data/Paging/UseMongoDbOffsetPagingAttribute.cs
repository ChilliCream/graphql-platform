using System;
using System.Reflection;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Pagination;

namespace HotChocolate.Types
{
    /// <summary>
    /// This attribute adds the offset paging middleware for mongodb to the annotated method or
    /// property.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Method)]
    public class UseMongoDbOffsetPagingAttribute : DescriptorAttribute
    {
        private int? _defaultPageSize;
        private int? _maxPageSize;
        private bool? _includeTotalCount;

        /// <summary>
        /// Applies the offset paging middleware to the annotated property.
        /// </summary>
        /// <param name="type">
        /// The schema type representing the item type.
        /// </param>
        public UseMongoDbOffsetPagingAttribute(Type? type = null)
        {
            Type = type;
        }

        /// <summary>
        /// The schema type representation of the item type.
        /// </summary>
        public Type? Type { get; }

        /// <summary>
        /// Specifies the default page size for this field.
        /// </summary>
        public int DefaultPageSize
        {
            get => _defaultPageSize ?? PagingDefaults.DefaultPageSize;
            set => _defaultPageSize = value;
        }

        /// <summary>
        /// Specifies the maximum allowed page size.
        /// </summary>
        public int MaxPageSize
        {
            get => _maxPageSize ?? PagingDefaults.MaxPageSize;
            set => _maxPageSize = value;
        }

        /// <summary>
        /// Include the total count field to the result type.
        /// </summary>
        public bool IncludeTotalCount
        {
            get => _includeTotalCount ?? PagingDefaults.IncludeTotalCount;
            set => _includeTotalCount = value;
        }

        protected override void TryConfigure(
            IDescriptorContext context,
            IDescriptor descriptor,
            ICustomAttributeProvider element)
        {
            if (descriptor is IObjectFieldDescriptor odf)
            {
                odf.UseMongoDbOffsetPaging(
                    Type,
                    options: new PagingOptions
                    {
                        DefaultPageSize = _defaultPageSize,
                        MaxPageSize = _maxPageSize,
                        IncludeTotalCount = _includeTotalCount
                    });
            }
        }
    }
}
