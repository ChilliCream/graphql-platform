using System;
using System.Reflection;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using static HotChocolate.ApolloFederation.ThrowHelper;

namespace HotChocolate.ApolloFederation
{
    /// <summary>
    /// The @provides directive is used to annotate the expected returned fieldset
    /// from a field on a base type that is guaranteed to be selectable by the gateway.
    ///
    /// <example>
    /// type Review @key(fields: "id") {
    ///   product: Product @provides(fields: "name")
    /// }
    ///
    /// extend type Product @key(fields: "upc") {
    ///   upc: String @external
    ///   name: String @external
    /// }
    /// </example>
    /// </summary>
    [AttributeUsage(
        AttributeTargets.Property |
        AttributeTargets.Method)]
    public sealed class ProvidesAttribute : DescriptorAttribute
    {
        /// <summary>
        /// Initializes a new instance of <see cref="ProvidesAttribute"/>.
        /// </summary>
        /// <param name="fieldSet">
        /// Gets the fields that is guaranteed to be selectable by the gateway.
        /// Grammatically, a field set is a selection set minus the braces.
        /// </param>
        public ProvidesAttribute(string fieldSet)
        {
            FieldSet = fieldSet;
        }

        /// <summary>
        /// Gets the fields that is guaranteed to be selectable by the gateway.
        /// Grammatically, a field set is a selection set minus the braces.
        /// </summary>
        public string FieldSet { get; }

        protected override void TryConfigure(
            IDescriptorContext context,
            IDescriptor descriptor,
            ICustomAttributeProvider element)
        {
            if (descriptor is IObjectFieldDescriptor ofd &&
                element is MemberInfo member)
            {
                if (FieldSet is null!)
                {
                    throw Provides_FieldSet_CannotBeEmpty(member);
                }

                ofd.Provides(FieldSet);
            }
        }
    }
}
