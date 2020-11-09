using System;
using System.Reflection;
using HotChocolate.Types.Descriptors;

#nullable enable

namespace HotChocolate.Types
{
    public sealed class UseDataLoaderAttribute : ObjectFieldDescriptorAttribute
    {
        private readonly Type _dataLoaderType;

        public UseDataLoaderAttribute(Type dataLoaderType)
        {
            _dataLoaderType = dataLoaderType;
        }

        public override void OnConfigure(
            IDescriptorContext context,
            IObjectFieldDescriptor descriptor,
            MemberInfo member)
        {
            descriptor.UseDataloader(_dataLoaderType);
        }
    }
}
