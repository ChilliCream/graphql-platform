using System;
using HotChocolate;
using HotChocolate.Stitching.DAPR;
using HotChocolate.Stitching.SchemaDefinitions;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class HotChocolateStitchingDAPRPublishSchemaDefinitionDescriptorExtensions
    {
        public static IPublishSchemaDefinitionDescriptor PublishToDAPR (
            this IPublishSchemaDefinitionDescriptor descriptor,
            NameString configurationName)
        {
            configurationName.EnsureNotEmpty(nameof(configurationName));

            return descriptor.SetSchemaDefinitionPublisher(sp =>
            {
                return new DAPRSchemaDefinitionPublisher(configurationName);
            });
        }
    }
}
