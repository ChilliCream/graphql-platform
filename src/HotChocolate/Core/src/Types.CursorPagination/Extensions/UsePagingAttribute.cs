using System;
using System.Reflection;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Pagination;

namespace HotChocolate.Types
{
    /// <summary>
    /// This attribute adds the cursor paging middleware to the annotated method or property.
    /// </summary>
    public sealed class UsePagingAttribute : DescriptorAttribute
    {
        private int? _defaultPageSize;
        private int? _maxPageSize;
        private bool? _includeTotalCount;
        private bool? _forward;
        private bool? _backward;

        /// <summary>
        /// Applies the offset paging middleware to the annotated property.
        /// </summary>
        /// <param name="type">
        /// The schema type representing the item type.
        /// </param>
        public UsePagingAttribute(Type? type = null)
        {
            Type = type;
        }

        /// <summary>
        /// The schema type representation of the item type.
        /// </summary>
        [Obsolete("Use Type.")]
        public Type? SchemaType { get => Type; set => Type = value; }

        /// <summary>
        /// The schema type representation of the item type.
        /// </summary>
        public Type? Type { get; private set; }

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

        /// <summary>
        /// Allow forward paging using <c>first</c> and <c>after</c>
        /// </summary>
        public bool? Forward
        {
            get => _forward;
            set => _forward = value;
        }

        /// <summary>
        /// Allow Backward paging using <c>last</c> and <c>before</c>
        /// </summary>
        public bool? Backward
        {
            get => _backward;
            set => _backward = value;
        }

        protected override void TryConfigure(
            IDescriptorContext context,
            IDescriptor descriptor,
            ICustomAttributeProvider element)
        {
            if (element is MemberInfo)
            {
                if (descriptor is IObjectFieldDescriptor ofd)
                {
                    ofd.UsePaging(
                        Type,
                        options: new PagingOptions
                        {
                            DefaultPageSize = _defaultPageSize,
                            MaxPageSize = _maxPageSize,
                            IncludeTotalCount = _includeTotalCount,
                            Backward = _backward,
                            Forward = _forward
                        });
                }
                else if (descriptor is IInterfaceFieldDescriptor ifd)
                {
                    ifd.UsePaging(
                        Type,
                        new PagingOptions
                        {
                            DefaultPageSize = _defaultPageSize,
                            MaxPageSize = _maxPageSize,
                            IncludeTotalCount = _includeTotalCount,
                            Backward = _backward,
                            Forward = _forward
                        });
                }
            }
        }
    }
}
