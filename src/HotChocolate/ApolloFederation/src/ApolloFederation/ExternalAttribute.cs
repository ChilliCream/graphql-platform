using System;
using System.Reflection;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.ApolloFederation
{
    /// <summary>
    /// The @external directive is used to mark a field as owned by another service.
    /// This allows service A to use fields from service B while also knowing at runtime
    /// the types of that field.
    ///
    /// <example>
    /// # extended from the Users service
    /// extend type User @key(fields: "email") {
    ///   email: String @external
    ///   reviews: [Review]
    /// }
    /// </example>
    /// </summary>
    public sealed class ExternalAttribute : ObjectFieldDescriptorAttribute
    {
        public override void OnConfigure(
            IDescriptorContext context,
            IObjectFieldDescriptor descriptor,
            MemberInfo member) =>
            descriptor.External();
    }
}
