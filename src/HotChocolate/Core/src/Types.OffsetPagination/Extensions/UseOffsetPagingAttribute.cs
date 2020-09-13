using System;
using System.Reflection;
using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types.Pagination
{
    public class UseOffsetPagingAttribute : DescriptorAttribute
    {
        protected override void TryConfigure(IDescriptorContext context, IDescriptor descriptor,
            ICustomAttributeProvider element)
        {
            throw new NotImplementedException();
        }
    }
}
