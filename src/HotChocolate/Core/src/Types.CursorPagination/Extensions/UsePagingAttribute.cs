using System;
using System.Reflection;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Pagination;

namespace HotChocolate.Types.Relay
{
    /// <summary>
    /// Applies a cursor paging middleware to a resolver.
    /// </summary>
    public sealed class UsePagingAttribute : DescriptorAttribute
    {
        public UsePagingAttribute(Type? type = null)
        {
            Type = type;
        }

        /// <summary>
        /// The schema type representation of the entity.
        /// </summary>
        [Obsolete("Use Type.")]
        public Type? SchemaType { get => Type; set => Type = value; }

        /// <summary>
        /// The schema type representation of the entity.
        /// </summary>
        public Type? Type { get; private set; }

        public int? DefaultPageSize { get; set; }

        public int? MaxPageSize { get; set; }

        public bool? IncludeTotalCount { get; set; }

        protected override void TryConfigure(
            IDescriptorContext context,
            IDescriptor descriptor,
            ICustomAttributeProvider element)
        {
            if (element is MemberInfo m)
            {
                if (descriptor is IObjectFieldDescriptor ofd)
                {
                    ofd.UsePaging(
                        Type,
                        settings: new PagingSettings
                        {
                            DefaultPageSize = DefaultPageSize,
                            MaxPageSize = MaxPageSize,
                            IncludeTotalCount = IncludeTotalCount
                        });
                }
                else if (descriptor is IInterfaceFieldDescriptor ifd)
                {
                    ifd.UsePaging(
                        Type,
                        new PagingSettings
                        {
                            DefaultPageSize = DefaultPageSize,
                            MaxPageSize = MaxPageSize,
                            IncludeTotalCount = IncludeTotalCount
                        });
                }
            }
        }
    }
}
