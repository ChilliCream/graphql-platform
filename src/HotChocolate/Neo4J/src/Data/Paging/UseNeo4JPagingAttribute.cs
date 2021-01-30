using System;
using System.Reflection;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Pagination;

namespace HotChocolate.Data.Neo4J.Paging
{
    /// <summary>
    /// This attribute adds the cursor paging middleware for Neo4J to the annotated method or
    /// property
    /// </summary>
    public sealed class UseNeo4JPagingAttribute : DescriptorAttribute
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
        public UseNeo4JPagingAttribute(Type? type = null)
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

        /// <inheritdoc />
        protected override void TryConfigure(
            IDescriptorContext context,
            IDescriptor descriptor,
            ICustomAttributeProvider element)
        {
            if (element is MemberInfo)
            {
                if (descriptor is IObjectFieldDescriptor ofd)
                {
                    ofd.UseNeo4JPaging(
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
}
