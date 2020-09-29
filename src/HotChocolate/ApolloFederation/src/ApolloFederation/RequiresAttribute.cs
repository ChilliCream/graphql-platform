using System;
using System.Reflection;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.ApolloFederation
{
    /// <summary>
    /// The @requires directive is used to annotate the required input fieldset
    /// from a base type for a resolver. It is used to develop a query plan
    /// where the required fields may not be needed by the client, but the
    /// service may need additional information from other services.
    ///
    /// <example>
    /// # extended from the Users service
    /// extend type User @key(fields: "id") {
    ///   id: ID! @external
    ///   email: String @external
    ///   reviews: [Review] @requires(fields: "email")
    /// }
    /// </example>
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class RequiresAttribute : ObjectFieldDescriptorAttribute
    {
        /// <summary>
        /// Initializes a new instance of <see cref="RequiresAttribute"/>.
        /// </summary>
        /// <param name="fieldSet">
        /// The <paramref name="fieldSet"/> describes which fields may
        /// not be needed by the client, but are required by
        /// this service as additional information from other services.
        /// Grammatically, a field set is a selection set minus the braces.
        /// </param>
        public RequiresAttribute(string fieldSet)
        {
            FieldSet = fieldSet;
        }

        /// <summary>
        /// Gets the fieldset which describes fields that may not be needed by the client,
        /// but are required by this service as additional information from other services.
        /// Grammatically, a field set is a selection set minus the braces.
        /// </summary>
        public string FieldSet { get; }

        public override void OnConfigure(
            IDescriptorContext context,
            IObjectFieldDescriptor descriptor,
            MemberInfo member)
        {
            if (FieldSet is null!)
            {
                // TODO : throw helper
                throw new SchemaException();
            }

            descriptor.Requires(FieldSet);
        }
    }
}
