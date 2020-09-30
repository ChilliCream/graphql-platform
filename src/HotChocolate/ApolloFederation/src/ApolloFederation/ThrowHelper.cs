using System;
using System.Reflection;
using HotChocolate.Types;
using static HotChocolate.ApolloFederation.Properties.FederationResources;

namespace HotChocolate.ApolloFederation
{
    /// <summary>
    /// This helper class provides a central place where we keep our exceptions so
    /// that they can be maintained more easily.
    /// </summary>
    internal static class ThrowHelper
    {
        /// <summary>
        /// Either the syntax node is invalid when parsing the literal or the syntax
        /// node value has an invalid format.
        /// </summary>
        public static SerializationException FieldSet_InvalidFormat(
            FieldSetType fieldSetType) =>
            new SerializationException(
                ErrorBuilder.New()
                    .SetMessage(ThrowHelper_FieldSet_HasInvalidFormat)
                    .SetCode(ErrorCodes.Scalars.InvalidSyntaxFormat)
                    .Build(),
                fieldSetType);

        /// <summary>
        /// The runtime type is not supported by the scalars ParseValue method.
        /// </summary>
        public static SerializationException FieldSet_CannotParseValue(
            FieldSetType fieldSetType,
            Type valueType) =>
            new SerializationException(
                ErrorBuilder.New()
                    .SetMessage(
                        ThrowHelper_FieldSet_CannotParseValue,
                        fieldSetType.Name,
                        valueType.FullName ?? valueType.Name)
                    .SetCode(ErrorCodes.Scalars.InvalidRuntimeType)
                    .Build(),
                fieldSetType);

        /// <summary>
        /// The schema doesn't contain any types with a key directive
        /// and therefore no entities. An Apollo federation service
        /// needs at least one entity.
        /// </summary>
        public static SchemaException EntityType_NoEntities() =>
            new SchemaException(
                SchemaErrorBuilder.New()
                    .SetMessage(ThrowHelper_EntityType_NoEntities)
                    .SetCode(ErrorCodes.Apollo.Federation.NoEntitiesDeclared)
                    .Build());

        /// <summary>
        /// The key attribute is used on the type level without specifying the the
        /// fieldset.
        /// </summary>
        public static SchemaException Key_FieldSet_CannotBeEmpty(
            Type type) =>
            new SchemaException(
                SchemaErrorBuilder.New()
                    .SetMessage(
                        ThrowHelper_Key_FieldSet_CannotBeEmpty,
                        type.FullName ?? type.Name)
                    .SetCode(ErrorCodes.Apollo.Federation.KeyFieldSetNullOrEmpty)
                    .Build());

        /// <summary>
        /// The provides attribute is used and the fieldset is set to <c>null</c> or
        /// <see cref="string.Empty"/>.
        /// </summary>
        public static SchemaException Provides_FieldSet_CannotBeEmpty(
            MemberInfo member) =>
            new SchemaException(
                SchemaErrorBuilder.New()
                    .SetMessage(ThrowHelper_Provides_FieldSet_CannotBeEmpty)
                    .SetCode(ErrorCodes.Apollo.Federation.ProvidesFieldSetNullOrEmpty)
                    .SetExtension(nameof(member), member)
                    .Build());

        /// <summary>
        /// The requires attribute is used and the fieldset is set to <c>null</c> or
        /// <see cref="string.Empty"/>.
        /// </summary>
        public static SchemaException Requires_FieldSet_CannotBeEmpty(
            MemberInfo member) =>
            new SchemaException(
                SchemaErrorBuilder.New()
                    .SetMessage(ThrowHelper_Requires_FieldSet_CannotBeEmpty)
                    .SetCode(ErrorCodes.Apollo.Federation.RequiresFieldSetNullOrEmpty)
                    .SetExtension(nameof(member), member)
                    .Build());
    }
}
