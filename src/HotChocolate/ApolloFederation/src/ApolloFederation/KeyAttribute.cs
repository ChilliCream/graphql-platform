using System;
using System.Reflection;
using HotChocolate.ApolloFederation.Extensions;
using HotChocolate.ApolloFederation.Properties;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.ApolloFederation
{
    /// <summary>
    /// The @key directive is used to indicate a combination of fields that
    /// can be used to uniquely identify and fetch an object or interface.
    /// <example>
    /// type Product @key(fields: "upc") {
    ///   upc: UPC!
    ///   name: String
    /// }
    /// </example>
    /// </summary>
    [AttributeUsage(
        AttributeTargets.Class |
        AttributeTargets.Interface |
        AttributeTargets.Property |
        AttributeTargets.Method)]
    public sealed class KeyAttribute : DescriptorAttribute
    {
        /// <summary>
        /// Initializes a new instance of <see cref="KeyAttribute"/>.
        /// </summary>
        /// <param name="fieldSet">
        /// Gets the field set that describes the key.
        /// Grammatically, a field set is a selection set minus the braces.
        /// </param>
        public KeyAttribute(string? fieldSet = default)
        {
            FieldSet = fieldSet;
        }

        /// <summary>
        /// Gets the field set that describes the key.
        /// Grammatically, a field set is a selection set minus the braces.
        /// </summary>
        public string? FieldSet { get; }

        protected override void TryConfigure(
            IDescriptorContext context,
            IDescriptor descriptor,
            ICustomAttributeProvider element)
        {
            if (descriptor is IInterfaceTypeDescriptor ifd)
            {
                if (FieldSet is null)
                {
                    // TODO : throw helper
                    throw new SchemaException();
                }

                ifd.Key(FieldSet);
            }

            if (descriptor is IObjectTypeDescriptor ad)
            {
                if (FieldSet is null)
                {
                    // TODO : throw helper
                    throw new SchemaException();
                }

                ad.Key(FieldSet);
            }

            if (descriptor is IObjectFieldDescriptor ofd)
            {
                ofd.Extend().OnBeforeCreate(
                    d =>
                    {
                        d.ContextData[WellKnownContextData.KeyMarker] = true;
                    }
                );
            }
        }
    }
}
