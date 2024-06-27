using System.Reflection;
using HotChocolate.ApolloFederation.Types;
using static HotChocolate.ApolloFederation.Properties.FederationResources;

namespace HotChocolate.ApolloFederation;

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
    /// Either the syntax node is invalid when parsing the literal or the syntax
    /// node value has an invalid format.
    /// </summary>
    public static SerializationException Any_InvalidFormat(
        _AnyType anyType) =>
        new SerializationException(
            ErrorBuilder.New()
                .SetMessage(ThrowHelper_Any_HasInvalidFormat)
                .SetCode(ErrorCodes.Scalars.InvalidSyntaxFormat)
                .Build(),
            anyType);

    /// <summary>
    /// The ExternalAttribute may only be applied to property members
    /// of a given type, which is not the case for the given member
    /// of the given type.
    /// </summary>
    public static SchemaException ExternalAttribute_InvalidTarget(
        Type type,
        MemberInfo? member) =>
        new SchemaException(
            SchemaErrorBuilder.New()
                .SetMessage(
                    "The specified external attribute was applied to " +
                    "the member `{0}` of `{1}`, which is not a property.",
                    member?.Name,
                    type.FullName ?? type.Name)
                .Build());

    /// <summary>
    /// No ReferenceResolver was found for a type that is decorated with a ReferenceResolverAttribute.
    /// </summary>
    public static SchemaException ReferenceResolverAttribute_EntityResolverNotFound(
        Type type,
        string referenceResolver) =>
        new SchemaException(
            SchemaErrorBuilder.New()
                .SetMessage(
                    "The specified ReferenceResolver `{0}` does not exist on `{1}`.",
                    type.FullName ?? type.Name,
                    referenceResolver)
                .Build());

    /// <summary>
    /// The runtime type is not supported by the scalars ParseValue method.
    /// </summary>
    public static SerializationException Scalar_CannotParseValue(
        ScalarType scalarType,
        Type valueType) =>
        new SerializationException(
            ErrorBuilder.New()
                .SetMessage(
                    ThrowHelper_Scalar_CannotParseValue,
                    scalarType.Name,
                    valueType.FullName ?? valueType.Name)
                .SetCode(ErrorCodes.Scalars.InvalidRuntimeType)
                .Build(),
            scalarType);

    /// <summary>
    /// The apollo gateway tries to resolve an entity for which no
    /// EntityResolver method was found.
    /// </summary>
    public static SchemaException EntityResolver_NoResolverFound() =>
        new SchemaException(
            SchemaErrorBuilder.New()
                .SetMessage(ThrowHelper_EntityResolver_NoEntityResolverFound)
                // .SetCode(ErrorCodes.ApolloFederation.NoEntityResolverFound)
                .Build());

    /// <summary>
    /// The key attribute is used on the type level without specifying the fieldset.
    /// </summary>
    public static SchemaException Key_FieldSet_CannotBeEmpty(
        Type type) =>
        new SchemaException(
            SchemaErrorBuilder.New()
                .SetMessage(
                    ThrowHelper_Key_FieldSet_CannotBeEmpty,
                    type.FullName ?? type.Name)
                // .SetCode(ErrorCodes.ApolloFederation.KeyFieldSetNullOrEmpty)
                .Build());

    /// <summary>
    /// The key attribute is used on the field level whilst specifying the fieldset.
    /// </summary>
    public static SchemaException Key_FieldSet_MustBeEmpty(
        MemberInfo member)
    {
        var type = member.ReflectedType ?? member.DeclaringType!;

        return new SchemaException(
            SchemaErrorBuilder.New()
                .SetMessage("The specified key attribute must not specify a fieldset when annotated to a field.")
                .SetExtension("type", type.FullName ?? type.Name)
                .SetExtension("member", member.Name)
                // .SetCode(ErrorCodes.ApolloFederation.KeyFieldSetNullOrEmpty)
                .Build());
    }

    /// <summary>
    /// The key attribute is used on the field level with inconsistent resolvable values.
    /// </summary>
    public static SchemaException Key_FieldSet_ResolvableMustBeConsistent(
        MemberInfo member)
    {
        var type = member.ReflectedType ?? member.DeclaringType!;

        return new SchemaException(
            SchemaErrorBuilder.New()
                .SetMessage(
                    "The specified key attributes must share the same resolvable " +
                    "values when annotated on multiple fields.")
                .SetExtension("type", type.FullName ?? type.Name)
                .SetExtension("member", member.Name)
                // .SetCode(ErrorCodes.ApolloFederation.KeyFieldSetNullOrEmpty)
                .Build());
    }

    /// <summary>
    /// The provides attribute is used and the fieldset is set to <c>null</c> or
    /// <see cref="string.Empty"/>.
    /// </summary>
    public static SchemaException Provides_FieldSet_CannotBeEmpty(
        MemberInfo member) =>
        new SchemaException(
            SchemaErrorBuilder.New()
                .SetMessage(ThrowHelper_Provides_FieldSet_CannotBeEmpty)
                // .SetCode(ErrorCodes.ApolloFederation.ProvidesFieldSetNullOrEmpty)
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
                // .SetCode(ErrorCodes.ApolloFederation.RequiresFieldSetNullOrEmpty)
                .SetExtension(nameof(member), member)
                .Build());

    /// <summary>
    /// The compose directive attribute is used on the type level without specifying the name.
    /// </summary>
    public static SchemaException ComposeDirective_Name_CannotBeEmpty(
        Type type) =>
        new SchemaException(
            SchemaErrorBuilder.New()
                .SetMessage(
                    ThrowHelper_ComposeDirective_Name_CannotBeEmpty,
                    type.FullName ?? type.Name)
                .Build());

    /// <summary>
    /// The link attribute is used on the schema without specifying the url.
    /// </summary>
    public static SchemaException Link_Url_CannotBeEmpty(
        Type type) =>
        new SchemaException(
            SchemaErrorBuilder.New()
                .SetMessage(
                    ThrowHelper_Link_Url_CannotBeEmpty,
                    type.FullName ?? type.Name)
                .Build());

    /// <summary>
    /// The contact attribute is used on the schema without specifying the name.
    /// </summary>
    public static SchemaException Contact_Name_CannotBeEmpty(
        Type type) =>
        new SchemaException(
            SchemaErrorBuilder.New()
                .SetMessage(
                    ThrowHelper_Contact_Name_CannotBeEmpty,
                    type.FullName ?? type.Name)
                .Build());

    /// <summary>
    /// Specified federation version is not supported.
    /// </summary>
    public static SchemaException FederationVersion_Unknown(
        FederationVersion version) =>
        new SchemaException(
            SchemaErrorBuilder.New()
                .SetMessage(
                    ThrowHelper_FederationVersion_Unknown,
                    version)
                .Build());

    public static SchemaException Contact_Not_Repeatable() =>
        new SchemaException(
            SchemaErrorBuilder.New()
                .SetMessage("The @contact directive is not repeatable and can.")
                .Build());
}
